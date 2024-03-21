using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Minio;
using Minio.DataModel;
using Minio.Exceptions;
using Minio.DataModel.Args;
using Google.Protobuf.WellKnownTypes;
using System.Reactive.Linq;
using System.Security.AccessControl;
using Newtonsoft.Json.Linq;
using System.Configuration;
using System.Security.Cryptography;
using System.Security.Principal;
using Org.BouncyCastle.Asn1.X509;

namespace rsync_stm
{
    public class Service
    {

        public void xlMsg(string json) 
        {
            try
            {
                var data = JsonConvert.DeserializeObject<InputSTM>(json);
                var json_id = "";
                
                if (data != null )
                {

                    //- Mở TKTT: 2003
                    //- Thay đổi thông tin: 2012
                    //- Nâng cấp gói tài khoản: 2013
                    //
                    json_id = insertJsonToDB(json);
                    if (data.service_type_id != 2003 && data.service_type_id != 2012 && data.service_type_id != 2013 && data.service_type_id != 2050) {
                        if (data.step_group_code == "SEND_OCR_INFO")
                        {
                            data.ext_info = xlCustomerInfo_TKTT(data.ext_info);
                        }
                    } else
                    if (data.service_type_id == 2003)
                    {
                        //- Mở TKTT: 2003
                        if (data.step_group_code == "SEND_OCR_INFO")
                        {
                            data.ext_info = xlCustomerInfo_TKTT(data.ext_info);
                        }
                        else if (data.step_group_code == "SEND_EFORM_INFO")
                        {
                            data.ext_info = xlRegisterAccount_TKTT(data.ext_info);
                        }
                        else if (data.step_group_code == "SUBMIT_EKYC_CONTRACT")
                        {
                            data.ext_info = xlEKYCContract_TKTT(data.ext_info);
                        }
                        else if (data.step_group_code == "SUBMIT_FORM_CONTRACT")
                        {
                            data.ext_info = xlEKYCContract_TKTT(data.ext_info);
                        }
                        else if (data.step_group_code == "NEW_OPEN_ACCOUNT")
                        {
                            // chỗ này kết quả, nên không cần check nữa, gửi thẳng lên client luôn
                        }

                    }
                    //- Thay đổi thông tin: 2012
                    else if (data.service_type_id == 2012)
                    {
                        if (data.step_group_code == "SEND_OCR_INFO")
                        {
                            data.ext_info = xlCustomerInfo_TKTT(data.ext_info);
                        }
                        else if (data.step_group_code == "SEND_EFORM_INFO")
                        {
                            data.ext_info = xlRegisterAccount_TDTT(data.ext_info);
                        }
                        else if (data.step_group_code == "SUBMIT_EKYC_CONTRACT")
                        {
                            data.ext_info = xlEKYCContract_TKTT(data.ext_info);
                        }
                        else if (data.step_group_code == "SUBMIT_FORM_CONTRACT")
                        {
                            data.ext_info = xlEKYCContract_TKTT(data.ext_info);
                        }
                        else if (data.step_group_code == "CUSTOMER_CHANGE_INFO")
                        {
                            // chỗ này kết quả, nên không cần check nữa, gửi thẳng lên client luôn
                        }
                    }
                    //- Nâng cấp gói tài khoản: 2013
                    else if (data.service_type_id == 2013)
                    {
                        if (data.step_group_code == "SEND_OCR_INFO")
                        {
                            data.ext_info = xlCustomerInfo_TKTT(data.ext_info);
                        }
                        else if (data.step_group_code == "SEND_EFORM_INFO")
                        {
                            data.ext_info = xlRegisterAccount_NCTK(data.ext_info);
                        }
                        else if (data.step_group_code == "SUBMIT_EKYC_CONTRACT")
                        {
                            data.ext_info = xlEKYCContract_TKTT(data.ext_info);
                        }
                        else if (data.step_group_code == "SUBMIT_FORM_CONTRACT")
                        {
                            data.ext_info = xlEKYCContract_TKTT(data.ext_info);
                        }
                        else if (data.step_group_code == "UPGRADE_KYC")
                        {
                            // chỗ này kết quả, nên không cần check nữa, gửi thẳng lên client luôn
                        }
                    }
                    //- Gọi nhỡ: 2050
                    else if (data.service_type_id == 2050)
                    {
                        if (data.step_group_code == "CALLBACK")
                        {
                            insertCallBack(data.ext_info);
                        }
                    }

                    if (data.ext_info != null && data.service_type_id != 2050)
                    {
                        var o_data = new
                        {
                            id = json_id,
                            data = data
                        };

                        var exchange = $"stm-{data.tid}";
                        string msg = Newtonsoft.Json.JsonConvert.SerializeObject(o_data);
                        Log.Information($"xlMsg send {msg}");
                        sendExchange(exchange, msg);
                    }

                }

            }
            catch (Exception e)
            {
                Log.Error($"xlMsg: {e.ToString()}");
            }           
        }

        public void xlSendMsg(string json)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<OutputSTM>(json);
                var json_id = "";

                if (data != null)
                {
                    json_id = insertJsonToDB(json);

                    // Kiểm tra xem chuỗi có chứa dấu "+" hay không
                    if (data.data.Contains("+"))
                    {
                        string[] parts = data.data.Split('+');

                        if (parts.Length >= 2)
                        {
                            string type = parts[0]; // Phần tử đầu tiên là "type"
                            string id = parts[1];   // Phần tử thứ hai là "id"

                            Log.Information($"Type: {type.ToString()}");
                            Log.Information($"ID: {id.ToString()}");

                            var send_data = getJsonfromDB(id);
                            var dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(send_data);
                            string updatedJson = "";
                            string api_url = "";
                            var newEntries = new Dictionary<string, object> { };
                            List<string> keysToRemove = new List<string> { };

                            Log.Information($"xlSendMsg: {send_data}");

                            //- Mở TKTT: 2003
                            //- Thay đổi thông tin: 2012
                            //- Nâng cấp gói tài khoản: 2013
                            
                            if (dictionary.ContainsKey("service_type_id"))
                            {
                                if (dictionary.ContainsKey("ext_info"))
                                {
                                    var extInfo = dictionary["ext_info"];
                                    var service_type_id = Convert.ToInt32(dictionary["service_type_id"]);
                                    var sessionId = dictionary.ContainsKey("session_id") ? dictionary["session_id"] : "";

                                    // Chuyển đổi extInfo thành Dictionary nếu nó là một object JSON
                                    if (extInfo is JObject extInfoObject)
                                    {
                                        var extInfoDict = extInfoObject.ToObject<Dictionary<string, object>>();

                                        var infoJsonId = getInfoJsonId(data.id);

                                        if (type == "SEND_OCR_INFO")
                                        {
                                            ModifiedJson modified_json_obj = JsonConvert.DeserializeObject<ModifiedJson>(infoJsonId.LASTEST);
                                            // Xóa các phần tử không mong muốn
                                            keysToRemove = new List<string> { "id", "step_group_code", "ext_info", "phone_no", "status_id" };

                                            // Danh sách các phần tử mới để thêm
                                            newEntries = new Dictionary<string, object>
                                            {
                                                { "status", "SUCCESS" },
                                                { "customer_name", modified_json_obj.customer_name},
                                                { "customer_id_no", modified_json_obj.customer_id_no },
                                                { "gender", modified_json_obj.gender },
                                                { "nationality", modified_json_obj.nationality },
                                                { "dob", modified_json_obj.dob },
                                                { "issue_date", modified_json_obj.issue_date },
                                                { "ebcardType", extInfoDict.ContainsKey("ebcardType") ? extInfoDict["ebcardType"] : "" },
                                                { "exprire_date", extInfoDict.ContainsKey("exprire_date") ? extInfoDict["exprire_date"] : "" },
                                                { "issue_place", modified_json_obj.issue_place },
                                                { "permanent_addr", modified_json_obj.permanent_addr },
                                            };

                                            api_url = "/core-service/api/message/send/update-ocr-info";

                                        } else if (type == "DONE_EDIT_OCR")
                                        {
                                            // EKYC_INFO
                                            /*
                                            var EKYC_INFO_FULL = getJsonfromDB(infoJsonId.EKYC_INFO);
                                            InputSTM EKYC_INFO_DATA_RAW = JsonConvert.DeserializeObject<InputSTM>(EKYC_INFO_FULL);
                                            EKYC_INFO EKYC_INFO_DATA = JsonConvert.DeserializeObject<EKYC_INFO>(EKYC_INFO_DATA_RAW.ToString());
                                            */

                                            // Xóa các phần tử không mong muốn
                                            keysToRemove = new List<string> { "id", "step_group_code", "ext_info" };

                                            // Danh sách các phần tử mới để thêm
                                            newEntries = new Dictionary<string, object>
                                                {
                                                    { "status", "DONE_EDIT_OCR" },
                                                    { "user_name", "" }, // Tên đăng nhập EKYC_INFO_DATA.accountNumber
                                                    { "acct_no", "" }, // Số tài khoản EKYC_INFO_DATA.idNumber
                                                    { "acct_name", "" }, // Tên tài khoản EKYC_INFO_DATA.name
                                                    { "cif_no", "" }, // Số CIF EKYC_INFO_DATA.cifNumber
                                                    { "ref_no", "" }, // Ref No/ID giao dịch EKYC_INFO_DATA.idNumber
                                                };

                                            /*
                                            // Thêm giá phần tử giá trị vào nêu khóa chưa có
                                            foreach (var entry in extInfoDict)
                                            {
                                                if (!newEntries.ContainsKey(entry.Key))
                                                {
                                                    newEntries.Add(entry.Key, entry.Value);
                                                }
                                                else
                                                {
                                                    // Ghi đè giá trị hiện tại hoặc thêm mới nếu khóa chưa tồn tại
                                                    // newEntries[entry.Key] = entry.Value;
                                                }
                                            }
                                            */
                                            api_url = "/core-service/api/message/send/result";
                                        }
                                        else if (type == "SEND_EFORM_INFO")
                                        {
                                            if (service_type_id == 2003)
                                            {
                                                ModifiedJson modified_json_obj = JsonConvert.DeserializeObject<ModifiedJson>(infoJsonId.LASTEST);
                                                // Xóa các phần tử không mong muốn
                                                keysToRemove = new List<string> { "id", "step_group_code", "ext_info", "status_id", "source_of_income_code", "pre_tax_income_code", "pre_tax_income_value" };

                                                // Danh sách các phần tử mới để thêm
                                                newEntries = new Dictionary<string, object>
                                                {
                                                    { "teller", data.agent },
                                                    { "print_type", "FORM" },
                                                    { "status", "APPROVE" },
                                                    { "customer_name", modified_json_obj.customer_name},
                                                    { "customer_id_no", modified_json_obj.customer_name },
                                                    { "gender", modified_json_obj.gender },
                                                    { "nationality", modified_json_obj.nationality },
                                                    { "dob", modified_json_obj.dob },
                                                    { "issue_date", modified_json_obj.issue_date },
                                                    { "issue_place", modified_json_obj.issue_place },
                                                    { "permanent_addr", modified_json_obj.permanent_addr },

                                                };

                                                // Thêm giá phần tử giá trị vào nêu khóa chưa có
                                                foreach (var entry in extInfoDict)
                                                {
                                                    if (!newEntries.ContainsKey(entry.Key))
                                                    {
                                                        newEntries.Add(entry.Key, entry.Value);
                                                    }
                                                    else
                                                    {
                                                        // Ghi đè giá trị hiện tại hoặc thêm mới nếu khóa chưa tồn tại
                                                        // newEntries[entry.Key] = entry.Value;
                                                    }
                                                }

                                                /*
                                                    { "email", extInfoDict.ContainsKey("email") ? extInfoDict["email"] : "" },
                                                    { "occupation", extInfoDict.ContainsKey("occupation") ? extInfoDict["occupation"] : "" },
                                                    { "job_title", extInfoDict.ContainsKey("job_title") ? extInfoDict["job_title"] : "" },
                                                    { "source_of_income", extInfoDict.ContainsKey("source_of_income") ? extInfoDict["source_of_income"] : "" },
                                                    { "pre_tax_income", extInfoDict.ContainsKey("pre_tax_income") ? extInfoDict["pre_tax_income"] : "" },
                                                    { "product_type", extInfoDict.ContainsKey("product_type") ? extInfoDict["product_type"] : "" },
                                                    { "user_name", extInfoDict.ContainsKey("user_name") ? extInfoDict["user_name"] : "" },
                                                    { "trading_addr", extInfoDict.ContainsKey("trading_addr") ? extInfoDict["trading_addr"] : "" },
                                                    { "secret_question", extInfoDict.ContainsKey("secret_question") ? extInfoDict["secret_question"] : "" },
                                                    { "referral_code", extInfoDict.ContainsKey("referral_code") ? extInfoDict["referral_code"] : "" },
                                                    { "fatca", extInfoDict.ContainsKey("fatca") ? extInfoDict["fatca"] : "" },
                                                    { "ustin", extInfoDict.ContainsKey("ustin") ? extInfoDict["ustin"] : "" },
                                                    { "current_addr", extInfoDict.ContainsKey("current_addr") ? extInfoDict["current_addr"] : "" },
                                                */
                                            }
                                            else if (service_type_id == 2012)
                                            {
                                                ModifiedJson modified_json_obj = JsonConvert.DeserializeObject<ModifiedJson>(infoJsonId.LASTEST);
                                                // Xóa các phần tử không mong muốn
                                                keysToRemove = new List<string> { "id", "step_group_code", "ext_info", "status_id", "source_of_income_code", "pre_tax_income_code", "pre_tax_income_value" };

                                                // EFORM_INFO
                                                var EFORM_INFO_FULL = getJsonfromDB(infoJsonId.SEND_EFORM_INFO);
                                                InputSTM EFORM_INFO_DATA = JsonConvert.DeserializeObject<InputSTM>(EFORM_INFO_FULL);
                                                RegisterAccount_NCTK EFORM_INFO_DATA_FINAL = JsonConvert.DeserializeObject<RegisterAccount_NCTK>(EFORM_INFO_DATA.ext_info.ToString());

                                                // EKYC_INFO
                                                var EKYC_INFO_FULL = getJsonfromDB(infoJsonId.EKYC_INFO);
                                                InputSTM EKYC_INFO_DATA_RAW = JsonConvert.DeserializeObject<InputSTM>(EKYC_INFO_FULL);
                                                EKYC_INFO EKYC_INFO_DATA = JsonConvert.DeserializeObject<EKYC_INFO>(EKYC_INFO_DATA_RAW.ToString());

                                                // Danh sách các phần tử mới để thêm
                                                newEntries = new Dictionary<string, object>
                                                {
                                                    { "teller", data.agent },
                                                    { "print_type", "FORM" },
                                                    { "status", "APPROVE" },
                                                    { "customer_name", modified_json_obj.customer_name},
                                                    { "customer_id_no", modified_json_obj.customer_name },
                                                    { "gender", modified_json_obj.gender },
                                                    { "nationality", modified_json_obj.nationality },
                                                    { "dob", modified_json_obj.dob },
                                                    { "issue_date", modified_json_obj.issue_date },
                                                    { "issue_place", modified_json_obj.issue_place },
                                                    { "permanent_addr", modified_json_obj.permanent_addr },
                                                    { "acct_no", EFORM_INFO_DATA_FINAL.acct_no }, // Số tài khoản
                                                    { "product_type", EFORM_INFO_DATA_FINAL.product_type }, // Loại tài khoản hiện tại. Mặc định “Gói TKTT online”
                                                    { "update_product_type", EFORM_INFO_DATA_FINAL.update_product_type }, // Loại tài khoản đề nghị chuyển đổi
                                                    { "updated_at", EFORM_INFO_DATA_FINAL.updated_at }, // Ngày đề nghị chuyển đổi (dd/mm/yyyy)

                                                };

                                                // Thêm giá phần tử giá trị vào nêu khóa chưa có
                                                foreach (var entry in extInfoDict)
                                                {
                                                    if (!newEntries.ContainsKey(entry.Key))
                                                    {
                                                        newEntries.Add(entry.Key, entry.Value);
                                                    }
                                                    else
                                                    {
                                                        // Ghi đè giá trị hiện tại hoặc thêm mới nếu khóa chưa tồn tại
                                                        // newEntries[entry.Key] = entry.Value;
                                                    }
                                                }
                                            }
                                            else if (service_type_id == 2013) {
                                                ModifiedJson modified_json_obj = JsonConvert.DeserializeObject<ModifiedJson>(infoJsonId.LASTEST);
                                                // Xóa các phần tử không mong muốn
                                                keysToRemove = new List<string> { "id", "step_group_code", "ext_info", "status_id", "source_of_income_code", "pre_tax_income_code", "pre_tax_income_value" };

                                                // EFORM_INFO
                                                var EFORM_INFO_FULL = getJsonfromDB(infoJsonId.SEND_EFORM_INFO);
                                                InputSTM EFORM_INFO_DATA = JsonConvert.DeserializeObject<InputSTM>(EFORM_INFO_FULL);
                                                RegisterAccount_NCTK EFORM_INFO_DATA_FINAL = JsonConvert.DeserializeObject<RegisterAccount_NCTK>(EFORM_INFO_DATA.ext_info.ToString());

                                                // EKYC_INFO
                                                var EKYC_INFO_FULL = getJsonfromDB(infoJsonId.EKYC_INFO);
                                                InputSTM EKYC_INFO_DATA_RAW = JsonConvert.DeserializeObject<InputSTM>(EKYC_INFO_FULL);
                                                EKYC_INFO EKYC_INFO_DATA = JsonConvert.DeserializeObject<EKYC_INFO>(EKYC_INFO_DATA_RAW.ToString());

                                                // Danh sách các phần tử mới để thêm
                                                newEntries = new Dictionary<string, object>
                                                {
                                                    { "teller", data.agent },
                                                    { "print_type", "FORM" },
                                                    { "status", "APPROVE" },
                                                    { "customer_name", modified_json_obj.customer_name},
                                                    { "customer_id_no", modified_json_obj.customer_name },
                                                    { "gender", modified_json_obj.gender },
                                                    { "nationality", modified_json_obj.nationality },
                                                    { "dob", modified_json_obj.dob },
                                                    { "issue_date", modified_json_obj.issue_date },
                                                    { "issue_place", modified_json_obj.issue_place },
                                                    { "permanent_addr", modified_json_obj.permanent_addr },
                                                    { "acct_no", EFORM_INFO_DATA_FINAL.acct_no }, // Số tài khoản
                                                    { "product_type", EFORM_INFO_DATA_FINAL.product_type }, // Loại tài khoản hiện tại. Mặc định “Gói TKTT online”
                                                    { "update_product_type", EFORM_INFO_DATA_FINAL.update_product_type }, // Loại tài khoản đề nghị chuyển đổi
                                                    { "updated_at", EFORM_INFO_DATA_FINAL.updated_at }, // Ngày đề nghị chuyển đổi (dd/mm/yyyy)

                                                };

                                                // Thêm giá phần tử giá trị vào nêu khóa chưa có
                                                foreach (var entry in extInfoDict)
                                                {
                                                    if (!newEntries.ContainsKey(entry.Key))
                                                    {
                                                        newEntries.Add(entry.Key, entry.Value);
                                                    }
                                                    else
                                                    {
                                                        // Ghi đè giá trị hiện tại hoặc thêm mới nếu khóa chưa tồn tại
                                                        // newEntries[entry.Key] = entry.Value;
                                                    }
                                                }
                                            }

                                            api_url = "/core-service/api/message/send/print-form";

                                        }
                                        else if (type == "SUBMIT_EKYC_CONTRACT")
                                        {
                                            if (service_type_id == 2003)
                                            {
                                            }
                                            else if (service_type_id == 2013 || service_type_id == 2012)
                                            {

                                                // EKYC_INFO
                                                var EKYC_INFO_FULL = getJsonfromDB(infoJsonId.EKYC_INFO);
                                                InputSTM EKYC_INFO_DATA_RAW = JsonConvert.DeserializeObject<InputSTM>(EKYC_INFO_FULL);
                                                EKYC_INFO EKYC_INFO_DATA = JsonConvert.DeserializeObject<EKYC_INFO>(EKYC_INFO_DATA_RAW.ToString());

                                                // Danh sách các phần tử mới để thêm
                                                newEntries = new Dictionary<string, object>
                                                {
                                                    { "status", "SAVED_PROFILE" },
                                                    { "user_name", EKYC_INFO_DATA.accountNumber }, // Tên đăng nhập
                                                    { "acct_no", EKYC_INFO_DATA.idNumber }, // Số tài khoản
                                                    { "acct_name", EKYC_INFO_DATA.name }, // Tên tài khoản
                                                    { "cif_no", EKYC_INFO_DATA.cifNumber }, // Số CIF
                                                    { "ref_no", EKYC_INFO_DATA.idNumber }, // Ref No/ID giao dịch
                                                };

                                                // Thêm giá phần tử giá trị vào nêu khóa chưa có
                                                foreach (var entry in extInfoDict)
                                                {
                                                    if (!newEntries.ContainsKey(entry.Key))
                                                    {
                                                        newEntries.Add(entry.Key, entry.Value);
                                                    }
                                                    else
                                                    {
                                                        // Ghi đè giá trị hiện tại hoặc thêm mới nếu khóa chưa tồn tại
                                                        // newEntries[entry.Key] = entry.Value;
                                                    }
                                                }
                                            }
                                            api_url = "/core-service/api/message/send/result";
                                        }
                                        else if (type == "SUBMIT_FORM_CONTRACT")
                                        {
                                            if (service_type_id == 2003)
                                            {
                                                // OCRInfo
                                                var OCR_INFO_FULL = getJsonfromDB(infoJsonId.SEND_OCR_INFO);
                                                InputSTM OCR_INFO_DATA = JsonConvert.DeserializeObject<InputSTM>(OCR_INFO_FULL);
                                                // OCR_INFO_DATA.ext_info = JsonConvert.DeserializeObject<CustomerInfo>(OCR_INFO_DATA.ext_info.ToString());
                                                CustomerInfo customerInfo = JsonConvert.DeserializeObject<CustomerInfo>(OCR_INFO_DATA.ext_info.ToString());

                                                if (customerInfo != null && customerInfo.gender != null)
                                                {
                                                    // Thay đổi giá trị của gender
                                                    switch (customerInfo.gender.ToLower()) {
                                                        case "nam":
                                                            customerInfo.gender = "1";
                                                            break;
                                                        case "nữ":
                                                            customerInfo.gender = "2";
                                                            break;
                                                        case "không xác định":
                                                            customerInfo.gender = "3";
                                                            break;
                                                    };


                                                    // Nếu bạn muốn cập nhật lại dữ liệu trong OCR_INFO_DATA.ext_info
                                                    OCR_INFO_DATA.ext_info = customerInfo;
                                                }

                                                // registerAccount
                                                var EFORM_INFO_FULL = getJsonfromDB(infoJsonId.SEND_EFORM_INFO);
                                                InputSTM EFORM_INFO_DATA = JsonConvert.DeserializeObject<InputSTM>(EFORM_INFO_FULL);
                                                EFORM_INFO_DATA.ext_info = JsonConvert.DeserializeObject<RegisterAccount>(EFORM_INFO_DATA.ext_info.ToString());

                                                // Danh sách các phần tử mới để thêm
                                                newEntries = new Dictionary<string, object>
                                                {
                                                    { "teller", data.agent },
                                                    { "cusOCRInfo", OCR_INFO_DATA.ext_info },
                                                    { "cusOnboard", EFORM_INFO_DATA.ext_info },
                                                };

                                                api_url = "/core-service/api/jupiter/registerAccount";

                                            } else if (service_type_id == 2013)
                                            {
                                                // Hồ sơ nâng cấp tài khoản
                                                // step_group_code = SUBMIT_FORM_CONTRACT
                                                // OCRInfo
                                                var OCR_INFO_FULL = getJsonfromDB(infoJsonId.SEND_OCR_INFO);
                                                InputSTM OCR_INFO_DATA = JsonConvert.DeserializeObject<InputSTM>(OCR_INFO_FULL);
                                                OCR_INFO_DATA.ext_info = JsonConvert.DeserializeObject<CustomerInfo>(OCR_INFO_DATA.ext_info.ToString());

                                                // Nâng cấp tài khoản
                                                var EFORM_INFO_FULL = getJsonfromDB(infoJsonId.SEND_EFORM_INFO);
                                                InputSTM EFORM_INFO_DATA = JsonConvert.DeserializeObject<InputSTM>(EFORM_INFO_FULL);
                                                EFORM_INFO_DATA.ext_info = JsonConvert.DeserializeObject<RegisterAccount_NCTK>(EFORM_INFO_DATA.ext_info.ToString());


                                                // Danh sách các phần tử mới để thêm
                                                newEntries = new Dictionary<string, object>
                                                {
                                                    { "teller", data.agent },
                                                    { "cusOCRInfo", OCR_INFO_DATA.ext_info },
                                                    { "cusUpgrade", EFORM_INFO_DATA.ext_info },
                                                };

                                                api_url = "/core-service/api/jupiter/ekyc/upgrade";

                                            } else if (service_type_id == 2012) {
                                                // Hồ sơ thay đổi thông tin
                                                // step_group_code = SUBMIT_FORM_CONTRACT
                                                // OCRInfo
                                                var OCR_INFO_FULL = getJsonfromDB(infoJsonId.SEND_OCR_INFO);
                                                InputSTM OCR_INFO_DATA = JsonConvert.DeserializeObject<InputSTM>(OCR_INFO_FULL);
                                                OCR_INFO_DATA.ext_info = JsonConvert.DeserializeObject<CustomerInfo>(OCR_INFO_DATA.ext_info.ToString());

                                                // Thay đổi thông tin
                                                var EFORM_INFO_FULL = getJsonfromDB(infoJsonId.SEND_EFORM_INFO);
                                                InputSTM EFORM_INFO_DATA = JsonConvert.DeserializeObject<InputSTM>(EFORM_INFO_FULL);
                                                EFORM_INFO_DATA.ext_info = JsonConvert.DeserializeObject<RegisterAccount_NCTK>(EFORM_INFO_DATA.ext_info.ToString());


                                                // Danh sách các phần tử mới để thêm
                                                newEntries = new Dictionary<string, object>
                                                {
                                                    { "teller", data.agent },
                                                    { "cusOCRInfo", OCR_INFO_DATA.ext_info },
                                                    { "cusChangeInfo", EFORM_INFO_DATA.ext_info },
                                                };

                                                api_url = "/core-service/api/jupiter/ekyc/customerChangeInfoRequest";

                                            }
                                        }
                                        else if (type == "NEW_OPEN_ACCOUNT")
                                        {
                                            api_url = "";
                                        }
                                        else if (type == "CUSTOMER_CHANGE_INFO")
                                        {
                                            api_url = "";
                                        }
                                        else if (type == "UPGRADE_KYC")
                                        {
                                            api_url = "";
                                        }
                                        else if (type == "TMP_EKYC")
                                        {
                                            // type TMP_EKYC này tự custom vì không có step_group_code riêng cho màn hình này
                                            // Nâng cấp tài khoản - 2013
                                            // Thay đổi thông tin - 2012
                                            // API Scant BM
                                            // print_type = EKYC
                                            // request theo Hồ sơ eKYC

                                            if (service_type_id == 2013 || service_type_id == 2012)
                                            {
                                                ModifiedJson modified_json_obj = JsonConvert.DeserializeObject<ModifiedJson>(infoJsonId.LASTEST);
                                                
                                                // EFORM_INFO
                                                var EFORM_INFO_FULL = getJsonfromDB(infoJsonId.SEND_EFORM_INFO);
                                                InputSTM EFORM_INFO_DATA = JsonConvert.DeserializeObject<InputSTM>(EFORM_INFO_FULL);
                                                RegisterAccount_NCTK EFORM_INFO_DATA_FINAL = JsonConvert.DeserializeObject<RegisterAccount_NCTK>(EFORM_INFO_DATA.ext_info.ToString());

                                                // EKYC_INFO
                                                var EKYC_INFO_FULL = getJsonfromDB(infoJsonId.EKYC_INFO);
                                                InputSTM EKYC_INFO_DATA_RAW = JsonConvert.DeserializeObject<InputSTM>(EKYC_INFO_FULL);
                                                EKYC_INFO EKYC_INFO_DATA = JsonConvert.DeserializeObject<EKYC_INFO>(EKYC_INFO_DATA_RAW.ToString());
                                                
                                                keysToRemove = new List<string> {"step_group_code"};
                                                
                                                // Danh sách các phần tử mới để thêm
                                                newEntries = new Dictionary<string, object>
                                                {
                                                    { "teller", data.agent },
                                                    { "print_type", "EKYC" },
                                                    { "status", "APPROVE" },
                                                    { "customer_name", modified_json_obj.customer_name},
                                                    { "customer_id_no", modified_json_obj.customer_name },
                                                    { "gender", modified_json_obj.gender },
                                                    { "nationality", modified_json_obj.nationality },
                                                    { "dob", modified_json_obj.dob },
                                                    { "issue_date", modified_json_obj.issue_date },
                                                    { "issue_place", modified_json_obj.issue_place },
                                                    { "permanent_addr", modified_json_obj.permanent_addr },
                                                    { "acct_no", EKYC_INFO_DATA.accountNumber }, // Số tài khoản
                                                    { "product_code", EKYC_INFO_DATA.accountProd }, // Mã sản phẩm
                                                    { "updated_product_code", EKYC_INFO_DATA.accountProd }, // Mã sản phẩm đề nghị đổi
                                                    { "source_of_income", EKYC_INFO_DATA.employmentStatus }, // Nguồn thu nhập
                                                    { "service_type", EKYC_INFO_DATA.serviceSms }, // Gói dịch vụ
                                                    { "is_notify", EKYC_INFO_DATA.smsAuto }, // Thông báo nhận biến động số dư
                                                    { "user_name", EKYC_INFO_DATA.idNumber }, // Tên truy cập
                                                    { "ib_service_type", EKYC_INFO_DATA.serviceIb }, // Gói dịch vụ IB
                                                    { "updated_ib_service_type", null }, // Gói dịch vụ IB đề nghị đổi
                                                    { "secret_type", EKYC_INFO_DATA.ibSecurityType }, // Hình thức bảo mật
                                                    { "mb_service", null }, // Dịch vụ Mobile Banking
                                                    { "mb_service_type", EKYC_INFO_DATA.serviceMb }, // Gói dịch vụ MB
                                                    { "updated_mb_service_type", null}, // Gói dịch vụ MB đề nghị đổi
                                                };

                                                // Thêm giá phần tử giá trị vào nêu khóa chưa có
                                                foreach (var entry in extInfoDict)
                                                {
                                                    if (!newEntries.ContainsKey(entry.Key))
                                                    {
                                                        newEntries.Add(entry.Key, entry.Value);
                                                    }
                                                    else
                                                    {
                                                        // Ghi đè giá trị hiện tại hoặc thêm mới nếu khóa chưa tồn tại
                                                        // newEntries[entry.Key] = entry.Value;
                                                    }
                                                }
                                            }
                                            api_url = "/core-service/api/message/send/print-form";
                                        }

                                        if (!string.IsNullOrEmpty(api_url))
                                        {
                                            // Xóa các phần tử không mong muốn
                                            foreach (var key in keysToRemove)
                                            {
                                                dictionary.Remove(key);
                                            }

                                            // Thêm các phần tử mới vào dictionary
                                            foreach (var entry in newEntries)
                                            {
                                                dictionary[entry.Key] = entry.Value; // Sử dụng cách này sẽ ghi đè nếu khóa đã tồn tại
                                            }

                                            // Chuyển đổi lại dictionary thành chuỗi JSON
                                            updatedJson = JsonConvert.SerializeObject(dictionary, Formatting.Indented);

                                            callAPISTM(api_url, updatedJson, data.agent, type, sessionId.ToString()).GetAwaiter().GetResult();
                                        }
                                        else {
                                            Log.Error($"api_url rỗng: {updatedJson}, step_group_code: {type}");
                                        }

                                        /*
                                        if (data.data != null)
                                        {
                                            var o_data = new
                                            {
                                                id = json_id,
                                                data = data
                                            };

                                            var exchange = $"stm-{data.id}";
                                            string msg = Newtonsoft.Json.JsonConvert.SerializeObject(o_data);
                                            Log.Information($"xlSendMsg send out {msg}");
                                            sendExchange(exchange, msg);
                                        }
                                        */
                                    }
                                    else {
                                        Log.Error($"ext_info sai định dạng: {dictionary["ext_info"]}");
                                    }
                                }
                                else {
                                    Log.Error($"Không có khóa ext_info");
                                }
                            }
                            else {
                                Log.Error($"Không có khóa service_type_id");
                            }
                        }
                        else
                        {
                            Log.Error($"Chuỗi phải chứa đủ 2 phần tử: {data.data.ToString()}");
                        }
                    }
                    else
                    {
                        Log.Error($"Chuỗi sai format, thiếu dấu '+': {data.data.ToString()}");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error data: {e.ToString()}");
                Log.Error($"xlMsg: {e.ToString()}");
            }
        }

        public MappingValue getUrlObj(string id)
        {
            try
            {
                MappingValue value = Program.mapping.First(x => x.id == id);
                return value;
            }
            catch (Exception e)
            {
                Log.Error($"getUrlObj: {e.ToString()}");
            }
            return null;
        }

        private CustomerInfo xlCustomerInfo_TKTT(Object obj)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<CustomerInfo>(obj.ToString());

                foreach (var item in data.images)
                {
                    item.url = getImages(item.minioPath);
                }

                return data;
            }
            catch (Exception e)
            {
                Log.Error($"xlCustomerInfo: {e.ToString()}");
            }

            return null;
        }
        private RegisterAccount xlRegisterAccount_TKTT(Object obj)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<RegisterAccount>(obj.ToString());

                return data;
            }
            catch (Exception e)
            {
                Log.Error($"xlRegisterAccount: {e.ToString()}");
            }

            return null;
        }
        private RegisterAccount_TDTT xlRegisterAccount_TDTT(Object obj)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<RegisterAccount_TDTT>(obj.ToString());

                return data;
            }
            catch (Exception e)
            {
                Log.Error($"xlRegisterAccount_TDTT: {e.ToString()}");
            }

            return null;
        }
        private RegisterAccount_NCTK xlRegisterAccount_NCTK(Object obj)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<RegisterAccount_NCTK>(obj.ToString());

                return data;
            }
            catch (Exception e)
            {
                Log.Error($"xlRegisterAccount_NCTK: {e.ToString()}");
            }

            return null;
        }
        private EKYCContract xlEKYCContract_TKTT(Object obj)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<EKYCContract>(obj.ToString());
                foreach (var item in data.images)
                {
                    item.url = getImages(item.minioPath);
                }
                return data;
            }
            catch (Exception e)
            {
                Log.Error($"xlEKYCContract: {e.ToString()}");
            }

            return null;
        }
        private KetQua xlKetQua(Object obj)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<KetQua>(obj.ToString());

                return data;
            }
            catch (Exception e)
            {
                Log.Error($"xlKetQua: {e.ToString()}");
            }

            return null;
        }
        private string getImages(string key)
        {
            try
            {
                // Các thiết lập khác giữ nguyên
                var endpoint = Program.appSettings.Minio.endpoint;
                var accessKey = Program.appSettings.Minio.accessKey;
                var secretKey = Program.appSettings.Minio.secretKey;
                var bucketName = "";
                var localFilePath = "";
                var relativeFilePath = "";

                string[] parts = key.Split(new char[] { '/' }, 2); // Chia chuỗi thành 2 phần

                if (parts.Length == 2)
                {
                    bucketName = parts[0]; // 'stmprocess'
                    string path = parts[1]; // Phần còn lại của đường dẫn

                    // Log để kiểm tra
                    Log.Information("Bucket Name: " + bucketName);
                    Log.Information("Path: " + path);

                    // Tạo thư mục theo ngày và giờ hiện tại
                    string dateFolder = DateTime.Now.ToString("yyyy-MM-dd");
                    string hourFolder = DateTime.Now.ToString("HH"); // Giờ hiện tại với định dạng 24 giờ
                    string directoryPath = Path.Combine(Program.appSettings.Minio.localPath, dateFolder, hourFolder);

                    // Kiểm tra nếu thư mục không tồn tại thì tạo mới
                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    // Đường dẫn cục bộ để lưu file, bao gồm thư mục ngày và giờ
                    localFilePath = Path.Combine(directoryPath, Path.GetFileName(path));

                    // Tạo một instance của MinioHelper
                    var minioHelper = new MinioHelper(endpoint, accessKey, secretKey);

                    // Gọi phương thức DownloadFileAsync và chờ hoàn thành
                    minioHelper.DownloadFileAsync(bucketName, path, localFilePath).GetAwaiter().GetResult();

                    // Tạo đường dẫn tương đối
                    relativeFilePath = Path.GetRelativePath(Program.appSettings.Minio.localPath, localFilePath);

                }
                else
                {
                    // Xử lý trường hợp chuỗi không đúng định dạng mong đợi
                    Log.Error($"Object khong dung dinh dang. {key}");
                    return "";
                }

                // Trả về đường dẫn cục bộ tới file sau khi tải xuống
                return relativeFilePath;
            }
            catch (Exception e)
            {
                Log.Error($"getImages: {e.ToString()}");
                return "";
            }
        }

        public class MinioHelper
        {
            private IMinioClient minioClient;

            public MinioHelper(string endpoint, string accessKey, string secretKey)
            {
                // Khởi tạo MinioClient
                this.minioClient = new MinioClient()
                                       .WithEndpoint(endpoint)
                                       .WithCredentials(accessKey, secretKey)
                                       .Build();
            }
            /*
            // Phương thức mới để liệt kê tất cả các đối tượng trong một bucket
            public void ListObjects(string bucketName)
            {
                try
                {
                    // Create ListObjectsArgs with the necessary information
                    ListObjectsArgs listObjectsArgs = new ListObjectsArgs()
                                                          .WithBucket(bucketName)
                                                          .WithRecursive(true);

                    // Subscribe to the observable returned by ListObjectsAsync
                    IDisposable subscription = minioClient.ListObjectsAsync(listObjectsArgs)
                        .Subscribe(
                            item => Log.Information($"Object: {item.Key}, Size: {item.Size}"),
                            ex => Log.Error($"Error occurred while listing objects: {ex.Message}"),
                            () => Log.Information("Finished listing objects.")
                        );

                    // Make sure to dispose the subscription when you're done
                    // to clean up resources. This could be when your application
                    // is shutting down or if you have a way to cancel the listing.
                }
                catch (Exception e)
                {
                    Log.Error($"Error occurred while listing objects: {e.Message}");
                }
            }
            */
            public async Task DownloadFileAsync(string bucketName, string objectName, string localPath)
            {
                try
                {
                    // Kiểm tra xem bucket có tồn tại
                    BucketExistsArgs bucketExistsArgs = new BucketExistsArgs()
                        .WithBucket(bucketName);
                    bool found = await minioClient.BucketExistsAsync(bucketExistsArgs);
                    if (!found)
                    {
                        Log.Warning($"Bucket does not exist.");
                        return;
                    }

                    // Phương thức mới để liệt kê tất cả các đối tượng trong một bucket
                    //ListObjects(bucketName);

                    // Tạo GetObjectArgs với thông tin cần thiết
                    GetObjectArgs getObjectArgs = new GetObjectArgs()
                                                    .WithBucket(bucketName)
                                                    .WithObject(objectName)
                                                    .WithFile(localPath);

                    // Get information about the object before downloading
                    StatObjectArgs statObjectArgs = new StatObjectArgs()
                                                        .WithBucket(bucketName)
                                                        .WithObject(objectName);
                    ObjectStat stat = await minioClient.StatObjectAsync(statObjectArgs);
                    Log.Information($"Object found - Name: {stat.ObjectName}, Size: {stat.Size}, LastModified: {stat.LastModified}");

                    // Now download the object
                    await minioClient.GetObjectAsync(getObjectArgs);
                    Log.Information($"File downloaded successfully to {localPath}");

                }
                catch (Exception e)
                {
                    Log.Error($"[MinioException] Error: {e.ToString()}, Message: {e.Message}");
                }
            }
        }

        private string insertJsonToDB(string json) {
            string id = Guid.NewGuid().ToString();
            try
            {
                
                string queryString = "INSERT INTO app_fd_sp_stm_json"
                + "(id,dateCreated,dateModified,createdBy,modifiedBy,"
                + "c_data)"
                + "VALUES (@id,now(),now(),@agent,@agent,"
                + $"@data)"
                ;

                using (var connection = ConnectionManager.GetConnection())
                {
                    MySqlCommand command = connection.CreateCommand();
                    command.CommandText = queryString;
                    command.Parameters.AddWithValue("@id", id);
                    command.Parameters.AddWithValue("@data", json);
                    command.Parameters.AddWithValue("@agent", "System");

                    command.ExecuteNonQuery();
                }
                

                Log.Information($"Insert Json id: {id}, Json: {json}");
            }
            catch (Exception e)
            {
                Log.Error($"insertJsonToDB: {e.ToString()}");
                id = "";
            }
            return id;
        }
        private void insertStatusAPIToDB(string url, string _body, dynamic rs, string agent, string type)
        {
            string id = Guid.NewGuid().ToString();
            try
            {

                string queryString = "INSERT INTO app_fd_sp_stm_logs"
                + "(id,dateCreated,dateModified,createdBy,modifiedBy,"
                + "c_rs,c_agent,c_body,c_url,c_step_group_code)"
                + "VALUES (@id,now(),now(),@agent,@agent,"
                + $"@rs,@agent,@body,@url,@step_group_code)"
                ;

                using (var connection = ConnectionManager.GetConnection())
                {
                    MySqlCommand command = connection.CreateCommand();
                    command.CommandText = queryString;
                    command.Parameters.AddWithValue("@id", id);
                    command.Parameters.AddWithValue("@agent", agent);
                    command.Parameters.AddWithValue("@rs", rs);
                    command.Parameters.AddWithValue("@body", _body);
                    command.Parameters.AddWithValue("@url", url);
                    command.Parameters.AddWithValue("@step_group_code", type);

                    command.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                Log.Error($"insertStatusAPIToDB: {e.ToString()}");
            }
        }
        private string getJsonfromDB(string id) {
            string data = "";
            try
            {   
                string queryString = "SELECT c_data FROM app_fd_sp_stm_json WHERE id = @id limit 1"
                ;

                using (var connection = ConnectionManager.GetConnection())
                {
                    MySqlCommand command = connection.CreateCommand();

                    command.CommandText = queryString;
                    command.Parameters.AddWithValue("@id", id);

                    MySqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        data = reader[0].ToString();
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"getJsonfromDB: {e.ToString()}");
                data = "";
            }
            return data;
        }
        private JsonId getInfoJsonId(string id)
        {
            JsonId data = new JsonId { };
            try
            {
                string queryString = "SELECT c_send_json, c_send_ocr_info_id, " +
                    "c_send_eform_info_id, c_submit_ekyc_contract_id, " +
                    "c_submit_form_contract_id, c_new_open_account_id, " +
                    "c_customer_change_info_id, c_upgrade_kyc_id, " +
                    "c_ekyc_info_id " +
                    "FROM app_fd_sp_stm_send WHERE id = @id limit 1"
                ;

                using (var connection = ConnectionManager.GetConnection())
                {
                    MySqlCommand command = connection.CreateCommand();

                    command.CommandText = queryString;
                    command.Parameters.AddWithValue("@id", id);

                    MySqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        data.LASTEST = reader[0].ToString();
                        data.SEND_OCR_INFO = reader[1].ToString();

                        data.SEND_EFORM_INFO = reader[2].ToString();
                        data.SUBMIT_EKYC_CONTRACT = reader[3].ToString();

                        data.SUBMIT_FORM_CONTRACT = reader[4].ToString();
                        data.NEW_OPEN_ACCOUNT = reader[5].ToString();

                        data.CUSTOMER_CHANGE_INFO = reader[6].ToString();
                        data.UPGRADE_KYC = reader[7].ToString();

                        data.EKYC_INFO = reader[8].ToString();
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"getInfoJsonId: {e.ToString()}");
                data = new JsonId { };
            }
            return data;
        }
        private async Task callAPISTM(string id, string body, string agent, string type, string sessionId) {
            try
            {
                var codeObj = getCodeObject(id);

                MappingValue valueHost = getUrlObj(id);

                if (valueHost == null)
                {
                    Log.Information($"callAPISTM: valueHost, url khong ton tai");
                    return;
                }

                if (string.IsNullOrEmpty(valueHost.token))
                {
                    valueHost.token = "eyJleHBpcmVUaW1lIjoiMjAyMzEwMTcxMzQ4MDYiLCJzZXJ2aWNlIjoiTkVXX0FUTSIsImxpdmVuZXNzIjoiMSIsImRldmljZUlkIjoiU1RNMDAxIn0=";
                }

                if (codeObj.type == "Json" && !string.IsNullOrEmpty(codeObj.token))
                {
                    // STM_Token token = await ql.stm_tokenAsync(codeObj.token)
                    STM_Token token = new STM_Token
                    {
                        access_token = valueHost.token
                        //access_token = "eyJleHBpcmVUaW1lIjoiMjAyMzEwMTcxMzQ4MDYiLCJzZXJ2aWNlIjoiTkVXX0FUTSIsImxpdmVuZXNzIjoiMSIsImRldmljZUlkIjoiU1RNMDAxIn0="
                    };

                    if(!string.IsNullOrEmpty(sessionId))
                    {
                        token.access_token = sessionId;
                    }

                    Log.Information($"Get Token STM: {token.access_token} ");
                    //string param = string.Join("&", ((JObject)data).Properties().Select(p => $"{p.Name}={p.Value}"));

                    string url = $"{codeObj.connstr}{valueHost.url}";
                    //string url = $"{codeObj.connstr}{id}";
                    /*
                    Log.Information($"Data request: {data}");
                    string jsonString;
                    if (data is JsonElement jsonElement)
                    {
                        jsonString = jsonElement.ToString();
                    }
                    else
                    {
                        jsonString = data; // Sử dụng trực tiếp nếu không phải là JsonElement
                    }
                    JObject jsonObject = JObject.Parse(jsonString);

                    string data_ = jsonObject.ToString();
                    */
                    dynamic rs = await getAPI_STM(url, token, body, valueHost.key);
                    insertStatusAPIToDB(url, body, rs, agent, type);
                    Log.Information($"getObject API return data: {rs}");
                }
            }
            catch (Exception e)
            {
                Log.Error($"callAPISTM: {e.ToString()}");
            }
        }

        public OutCodeObject getCodeObject(string code)
        {
            OutCodeObject outCodeObject = new OutCodeObject();
            try
            {
                string queryString = "select c.id, c.c_query, c.c_temp, cn.c_type, cn.c_connectionString, cn.c_token from jwdb.app_fd_sp_core c join jwdb.app_fd_sp_core_connstr cn on c.c_type = cn.id where c.id = @Id limit 1";

                using (var connection = ConnectionManager.GetConnection())
                {
                    MySqlCommand command = connection.CreateCommand();

                    command.CommandText = queryString;
                    command.Parameters.AddWithValue("@Id", code);

                    MySqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        outCodeObject = new OutCodeObject()
                        {
                            id = reader[0].ToString()
                            ,
                            query = reader[1].ToString()
                            ,
                            temp = reader[2].ToString()
                            ,
                            type = reader[3].ToString()
                            ,
                            connstr = reader[4].ToString()
                            ,
                            token = reader[5].ToString()
                        };
                    }

                    reader.Close();
                    connection.Close();
                    return outCodeObject;
                }
            }
            catch (Exception e)
            {
                Log.Error("getCodeObject: " + e.ToString());
            }
            return null;
        }

        public async Task<object> getAPI_STM(string url, STM_Token o, string jsonData_request, string key)
        {
            try
            {

                using (var client = new HttpClient())
                {
                    // Thiết lập headers
                    client.DefaultRequestHeaders.Add("sessionId", o.access_token);
                    client.DefaultRequestHeaders.Add("cache-control", "no-cache");
                    client.DefaultRequestHeaders.Add("Msb-Api-Key", key);

                    // Tạo content cho request
                    var content = new StringContent(jsonData_request, Encoding.UTF8, "application/json");

                    // Gửi POST request
                    var response = await client.PostAsync(url, content);

                    // Đọc và xử lý phản hồi
                    string responseContent = await response.Content.ReadAsStringAsync();
                    Log.Error("responseContent: " + responseContent.ToString());
                    object result = JsonConvert.DeserializeObject<object>(responseContent);

                    Log.Information($"POST thành công data");
                    return result;
                }
            }
            catch (Exception e)
            {
                Log.Error("getAPI_STM: " + e.ToString());
            }
            return null;

        }
        private void sendExchange(string exchange, string message)
        {
            try
            {
                var HostName = Program.appSettings.RabbitMQ.Hostname;
                var factory = new ConnectionFactory() { Port = Program.appSettings.RabbitMQ.Port };
                if (!string.IsNullOrEmpty(Program.appSettings.RabbitMQ.username) && !string.IsNullOrEmpty(Program.appSettings.RabbitMQ.password))
                {
                    factory.UserName = Program.appSettings.RabbitMQ.username;
                    factory.Password = Program.appSettings.RabbitMQ.password;
                }
                using (var connection = factory.CreateConnection(HostName))
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare(exchange: exchange,
                                         durable: true,
                                         type: "direct",
                                         autoDelete: false,
                                         arguments: null);
                    var body = Encoding.UTF8.GetBytes(message);
                    channel.BasicPublish(exchange: exchange,
                                         routingKey: "",
                                         basicProperties: null,
                                         body: body);
                    channel.BasicQos(0, 10, false);
                }
            }
            catch (Exception e)
            {
                Log.Error($"sendExchange: {e.ToString()}");
            }
        }
        private string insertCallBack(Object callback_info)
        {
            string id = Guid.NewGuid().ToString();
            try
            {
                var data = JsonConvert.DeserializeObject<CallBackInfo>(callback_info.ToString());
                /*
                private int type = 1;
                private String teller;
                private String sessionId;
                private String personRequest;
                private String customerSegment;
                private String phone;
                private String missCallNumber;
                private DateTime callDate;
                private String branchService;
                private String status;
                private String level;
                private String dataSource;
                private String clientType;
                private String cif;
                private String cardId;
                private DateTime dateOfBirth;
                private DateTime createdDate;
                private String createdBy;
                private DateTime updatedDate;
                private String updatedBy;
                */

                string queryString = "INSERT INTO app_fd_sp_stm_callback"
                + "(id,dateCreated,dateModified,createdBy,modifiedBy,"
                + "c_nguon_yeu_cau, c_nhanh_phan_khuc, c_so_goi_den, c_so_lan_nho,"
                + "c_thoi_gian_goi_den, c_nhanh_dich_vu, c_tinh_trang, c_muc_do,"
                + "c_nguon_du_lieu, c_individual) "
                + "VALUES (@id,now(),now(),@agent,@agent,"
                + $"@nguon_yeu_cau, @nhanh_phan_khuc, @so_goi_den, @so_lan_nho,"
                + $"@thoi_gian_goi_den, @nhanh_dich_vu, @tinh_trang, @muc_do,"
                + $"@nguon_du_lieu, @individual)"
                ;

                using (var connection = ConnectionManager.GetConnection())
                {
                    MySqlCommand command = connection.CreateCommand();
                    command.CommandText = queryString;
                    command.Parameters.AddWithValue("@id", id);
                    command.Parameters.AddWithValue("@agent", data.teller);
                    command.Parameters.AddWithValue("@nguon_yeu_cau", data.personRequest);
                    command.Parameters.AddWithValue("@nhanh_phan_khuc", "MASS");
                    command.Parameters.AddWithValue("@so_goi_den", data.phone);
                    command.Parameters.AddWithValue("@so_lan_nho", data.missCallNumber);
                    command.Parameters.AddWithValue("@thoi_gian_goi_den", data.callDate);
                    command.Parameters.AddWithValue("@nhanh_dich_vu", "Video call STM");
                    command.Parameters.AddWithValue("@tinh_trang", "Mở mới");
                    command.Parameters.AddWithValue("@muc_do", "HIGH");
                    command.Parameters.AddWithValue("@nguon_du_lieu", "STM");
                    command.Parameters.AddWithValue("@individual", "Cá nhân");

                    command.ExecuteNonQuery();
                }


                Log.Information($"Insert CallBack: {id}");
            }
            catch (Exception e)
            {
                Log.Error($"insertCallBack: {e.ToString()}");
                id = "";
            }
            return id;
        }
    }
}
