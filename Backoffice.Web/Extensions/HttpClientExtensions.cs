namespace Backoffice.Web.Extensions
{
    public static class HttpClientExtensions
    {
        public static HttpClient WithRequestHeader(this HttpClient client, Dictionary<string, string> content)
        {
            foreach (var item in content)
            {
                if (client.DefaultRequestHeaders.Contains(item.Key))
                    client.DefaultRequestHeaders.Remove(item.Key);

                client.DefaultRequestHeaders.Add(item.Key, item.Value);
            }

            return client;
        }
    }
}
