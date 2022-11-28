namespace Backoffice.Web.Models
{
    public class ModalParameters
    {
        public delegate Task ModalCallback(object parameter);
        private Dictionary<string, object> _parameters;
        private ModalCallback _modalCallback;
        public ModalParameters()
        {
            _parameters = new Dictionary<string, object>();
        }

        public void Add(string parameterName, object value)
        {
            _parameters[parameterName] = value;
        }

        public void SetCallback(ModalCallback modalCallback)
        {
            _modalCallback = modalCallback;
        }

        public async Task InvokeCallback(object parameter)
        {
            await _modalCallback.Invoke(parameter);
        }

        public T Get<T>(string parameterName)
        {
            if (!_parameters.ContainsKey(parameterName))
            {
                throw new KeyNotFoundException("Doesn not exist");
            }

            return (T)_parameters[parameterName];
        }

        public T TryGet<T>(string parameterName)
        {
            if (_parameters.ContainsKey(parameterName))
            {
                return (T)_parameters[parameterName];
            }

            return default;
        }
    }
}
