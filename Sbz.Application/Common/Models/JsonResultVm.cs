namespace Sbz.Application.Common.Models
{
    public class JsonResultVm<T>
    {
        public JsonResultVm(T result = default)
        {
            Message = "";
            HasError = false;
            Status = 200;
            Result = result;
        }
        public JsonResultVm(bool hasError, string message = "", T result = default, int status = 200)
        {
            Message = message;
            HasError = hasError;
            Status = status;
            Result = result;
        }

        public string Message { get; set; }
        public bool HasError { get; set; }
        public int Status { get; set; }
        public T Result { get; set; }
    }

    public class JsonResultVm
    {
        public JsonResultVm(bool hasError, string message = "", int status = 200)
        {
            Message = message;
            HasError = hasError;
            Status = status;
        }
        public int Status { get; set; }
        public string Message { get; set; }
        public bool HasError { get; set; }
    }
}
