using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.File;

namespace FunctionApp1
{
    public static class GetSasTokenForFileshare
    {
        [FunctionName("GetSasTokenForFileshare")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            // parse query parameter
            dynamic data = await req.Content.ReadAsAsync<object>();

            if (data.fileshare == null)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, new
                {
                    error = "Specify value for 'fileshare'"
                });
            }

            var permissions = SharedAccessFilePermissions.Read; //SharedAccessBlobPermissions.Read; // default to read permissions
            bool success = Enum.TryParse(data.permissions.ToString(), out permissions);

            if (!success)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, new
                {
                    error = "Invalid value for 'permissions'"
                });
            }

            var storageAccount = CloudStorageAccount.Parse("");// Environment.GetEnvironmentVariable["AzureWebJobsStorage"]);
            var fileClient = storageAccount.CreateCloudFileClient();
            var container = fileClient.GetShareReference(data.fileshare.ToString());

            var sasToken =
                //data.blobName != null ?
                    GetFileSasToken(container, data.blobName.ToString(), permissions);
                   // GetContainerSasToken(container, permissions);

            return req.CreateResponse(HttpStatusCode.OK, new
            {
                token = sasToken,
                uri = container.Uri + sasToken
            });

        }


        public static string GetFileSasToken(CloudFileShare fileshare, string blobName, SharedAccessFilePermissions permissions, string policyName = null)
        {
            string sasFileToken;

            // Get a reference to a blob within the container.
            // Note that the blob may not exist yet, but a SAS can still be created for it.
           // CloudBlockBlob blob = container.GetBlockBlobReference(blobName);
            //CloudFileDirectory dir= fileshare.GetRootDirectoryReference()

            if (policyName == null)
            {
                var adHocSas = CreateAdHocSasPolicy(permissions);

                // Generate the shared access signature on the blob, setting the constraints directly on the signature.
                sasFileToken = fileshare.GetSharedAccessSignature(adHocSas);
            }
            else
            {
                // Generate the shared access signature on the blob. In this case, all of the constraints for the
                // shared access signature are specified on the container's stored access policy.
                sasFileToken = fileshare.GetSharedAccessSignature(null, policyName);
            }

            return sasFileToken;
        }

        //public static string GetContainerSasToken(CloudBlobContainer container, SharedAccessBlobPermissions permissions, string storedPolicyName = null)
        //{
        //    string sasContainerToken;

        //    // If no stored policy is specified, create a new access policy and define its constraints.
        //    if (storedPolicyName == null)
        //    {
        //        var adHocSas = CreateAdHocSasPolicy(permissions);

        //        // Generate the shared access signature on the container, setting the constraints directly on the signature.
        //        sasContainerToken = container.GetSharedAccessSignature(adHocSas, null);
        //    }
        //    else
        //    {
        //        // Generate the shared access signature on the container. In this case, all of the constraints for the
        //        // shared access signature are specified on the stored access policy, which is provided by name.
        //        // It is also possible to specify some constraints on an ad-hoc SAS and others on the stored access policy.
        //        // However, a constraint must be specified on one or the other; it cannot be specified on both.
        //        sasContainerToken = container.GetSharedAccessSignature(null, storedPolicyName);
        //    }

        //    return sasContainerToken;
        //}

        private static SharedAccessFilePolicy CreateAdHocSasPolicy(SharedAccessFilePermissions permissions)
        {
            // Create a new access policy and define its constraints.
            // Note that the SharedAccessBlobPolicy class is used both to define the parameters of an ad-hoc SAS, and 
            // to construct a shared access policy that is saved to the container's shared access policies. 

            return new SharedAccessFilePolicy()
            {
                // Set start time to five minutes before now to avoid clock skew.
                SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-5),
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(1),
                Permissions = permissions
            };
        }
    }
}
