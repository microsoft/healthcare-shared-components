using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Health.SqlServer
{
    public class WorkloadIdentityAccessTokenHandler: IAccessTokenHandler
    {
        private const string AzureResource = "https://database.windows.net/.default";

        public SqlServerAuthenticationType AuthenticationType => SqlServerAuthenticationType.WorkloadIdentity;

        public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            WorkloadIdentityCredential credential = new WorkloadIdentityCredential();

            var token = await credential.GetTokenAsync(new TokenRequestContext(new[] { AzureResource }), CancellationToken.None).ConfigureAwait(false);

            return token.Token;
        }
    }
}
