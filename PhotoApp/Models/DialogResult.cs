namespace PhotoApp.Models
{
    public class DialogResultWithData
    {
        public const int RESULT_CANCEL = 0;
        public const int RESULT_OK = 1;
        public object Data { get; private set; }
        public int ResultCode { get; private set; }
        public DialogResultWithData(object data, int code)
        {
            Data = data;
            ResultCode = code;
        }
    }
}
