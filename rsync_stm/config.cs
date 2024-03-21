using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System.Xml.Linq;

namespace rsync_stm
{
    internal class config
    {
        public MySQL MySQL { get; set; }
        public Rabbitmq RabbitMQ { get; set; }
        public string PostgreSQLConnection { get; set; }
        public Minio Minio { get; set; }
    }
    public class MySQL
    {
        public string connectstr { get; set; }
        public int maxPoolSize { get; set; }
    }

    public class Rabbitmq
    {
        public string[] Hostname { get; set; }
        public int Port { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string Queue_CRM { get; set; }
        public string Queue_SEND { get; set; }
        public string Queue_RsyncData { get; set; }
        public string Queue_Email { get; set; }
        public string Queue_NTF { get; set; }
    }
    public class Minio
    {
        public string endpoint { get; set; }
        public string accessKey { get; set; }
        public string secretKey { get; set; }
        public string bucketName { get; set; }
        public string localPath { get; set; }
    }

    public class CustomerInfo
    {
        public string gender { get; set; }
        public string nationality { get; set; }
        public string dob { get; set; }
        public string issue_date { get; set; }
        public string issue_place { get; set; }
        public string permanent_addr { get; set; }
        public string front_img_id { get; set; }
        public string back_img_id { get; set; }
        public string face_img_id { get; set; }
        public string ebcardType { get; set; }
        public string exprire_date { get; set; }
        public List<Images> images { get; set; }
    }
    public class KetQua
    {
        public string timestamp { get; set; }
        public string code { get; set; }
        public string message { get; set; }
        public string traceId { get; set; }
        public Object infoError { get; set; }
    }
    public class EKYCContract
    {
        public List<Images> images { get; set; }
    }
    public class Images
    {
        public string type { get; set; }
        public string label { get; set; }
        public string imageId { get; set; }
        public string minioPath { get; set; }
        public string url { get; set; }
    }
    public class RegisterAccount
    {
        public string email { get; set; }
        public string occupation { get; set; }
        public string job_title { get; set; }
        public string source_of_income { get; set; }
        public string source_of_income_code { get; set; }
        public string source_of_income_value { get; set; }
        public string pre_tax_income { get; set; }
        public string pre_tax_income_code { get; set; }
        public string pre_tax_income_value { get; set; }
        public string product_type { get; set; }
        public string user_name { get; set; }
        public string trading_addr { get; set; }
        public string secret_question { get; set; }
        public string referral_code { get; set; }
        public bool fatca { get; set; }
        public string ustin { get; set; }
        public string addressContact { get; set; }
        public string current_addr { get; set; }
    }
    public class RegisterAccount_TDTT
    {
        public string resident_addr { get; set; }
        public string current_addr { get; set; }
        public string email_service { get; set; }
        public bool fatca { get; set; }
        public string ustin { get; set; }
    }
    public class RegisterAccount_NCTK
    {
        public string acct_no { get; set; }
        public string product_type { get; set; }
        public string update_product_type { get; set; }
        public string updated_at { get; set; }
    }

    public class InputSTM
    {
        public string id { get; set; }
        public string tid { get; set; }
        public string trx_id { get; set; }
        public string session_id { get; set; }
        public int service_type_id { get; set; }
        public string step_group_code { get; set; }
        public string customer_id_no { get; set; }
        public string customer_name { get; set; }
        public string phone_no { get; set; }
        public int status_id { get; set; }
        public string session { get; set; }
        public Object ext_info { get; set; }
    }

    public class OutputSTM
    {
        public string id { get; set; }
        public string data { get; set; }
        public string agent { get; set; }
    }

    public class OutCodeObject
    {
        public string id { get; set; }
        public string type { get; set; }
        public string query { get; set; }
        public string temp { get; set; }
        public string connstr { get; set; }
        public string token { get; set; }
    }

    public class STM_Token
    {
        public string access_token { get; set; }
        public string expires_in { get; set; }
    }
    public class ModifiedJson
    {
        public string customer_name { get; set; }
        public string customer_id_no { get; set; }
        public string gender { get; set; }
        public string nationality { get; set; }
        public string dob { get; set; }
        public string issue_date { get; set; }
        public string issue_place { get; set; }
        public string permanent_addr { get; set; }
        public string teller { get; set; }
    }

    public class EKYC_INFO
    {
        public string name { get; set; } // Tên khách hàng
        public string birthdate { get; set; } // Ngày sinh
        public string gender { get; set; } // Giới tính
        public string cifNumber { get; set; } // Số CIF
        public string idNumber { get; set; } // Số giấy tờ tùy thân
        public string idDate { get; set; } // 
        public string address { get; set; } // Địa chỉ thường trú
        public string addressContact { get; set; } // Địa chỉ liên lạc (tạm trú)
        public string mobilePhone { get; set; } // Số điện thoại di động
        public string email { get; set; } // Email
        public string employmentStatus { get; set; } // Nguồn thu nhập
        public string accountNumber { get; set; } // Số tài khoản
        public string accountProd { get; set; } // Mã sản phẩm tài khoản
        public string serviceSms { get; set; } // Gói dịch vụ
        public string smsAuto { get; set; } // Có/không
        public string smsMobilePhone { get; set; } // Số ĐT
        public string serviceIb { get; set; } // Gói dịch vụ IB
        public string serviceMb { get; set; } // Gói dịch vụ MB
        public string ibMb { get; set; } // Có/không
        public string ibGroupServCr { get; set; }
        public string mbGroupServCr { get; set; }
        public string ibSecurityType { get; set; } // Hình thức bảo mật (SMS,…)
        public string images { get; set; } // Thông tin ảnh
        public string minioPath { get; set; } // Đường dẫn ảnh
        public string label { get; set; }

    }
    public class JsonId
    {
        public string LASTEST { get; set; }
        public string SEND_OCR_INFO { get; set; }
        public string SEND_EFORM_INFO { get; set; }
        public string SUBMIT_EKYC_CONTRACT { get; set; }
        public string SUBMIT_FORM_CONTRACT { get; set; }
        public string NEW_OPEN_ACCOUNT { get; set; }
        public string CUSTOMER_CHANGE_INFO { get; set; }
        public string UPGRADE_KYC { get; set; }
        public string EKYC_INFO { get; set; }
    }
    public class CallBackInfo
    {
        public int type;
        public string teller;
        public string sessionId;
        public string personRequest;
        public string customerSegment;
        public string phone;
        public string missCallNumber;
        public string callDate;
        public string branchService;
        public string status;
        public string level;
        public string dataSource;
        public string clientType;
        public string cif;
        public string cardId;
        public string dateOfBirth;
        public string createdDate;
        public string createdBy;
        public string updatedDate;
        public string updatedBy;
    }

    public class MappingValues
    {
        public List<MappingValue> mappings { get; set; }
    }

    public class MappingValue
    {
        public string name { get; set; }
        public string id { get; set; }
        public string url { get; set; }
        public string key { get; set; }
        public string token { get; set; }
    }


}
