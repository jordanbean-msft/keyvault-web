using Azure.Core;
using Azure.Identity;
using Microsoft.Configuration.ConfigurationBuilders;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Web;

namespace web_net_framework.Services
{
    public class CertificateAuthenticationAzureKeyVaultConfigBuilder : AzureKeyVaultConfigBuilder
    {
        public const string certificateStoreNameTag = "certificateStoreName";
        public const string certificateStoreLocationTag = "certificateStoreLocation";
        public const string certificateThumbprintTag = "certificateThumbprint";
        public const string tenantIdTag = "tenantId";
        public const string clientIdTag = "clientId";
        public string CertificateStoreName { get; protected set; }
        public string CertificateStoreLocation { get; protected set; }
        public string CertificateThumbprint { get; protected set; }
        public string TenantId { get; protected set; }
        public string ClientId { get; protected set; }

        protected override void LazyInitialize(string name, NameValueCollection config)
        {
            CertificateStoreName = UpdateConfigSettingWithAppSettings(certificateStoreNameTag);
            CertificateStoreLocation = UpdateConfigSettingWithAppSettings(certificateStoreLocationTag);
            CertificateThumbprint = UpdateConfigSettingWithAppSettings(certificateThumbprintTag);
            TenantId = UpdateConfigSettingWithAppSettings(tenantIdTag);
            ClientId = UpdateConfigSettingWithAppSettings(clientIdTag);

            base.LazyInitialize(name, config);
        }

        protected override TokenCredential GetCredential()
        {
            StoreName storeName;
            try
            {
                storeName = (StoreName)Enum.Parse(typeof(StoreName), CertificateStoreName);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Unable to parse certificate store name: {CertificateStoreName}", ex);
            }

            StoreLocation storeLocation;
            try
            {
                storeLocation = (StoreLocation)Enum.Parse(typeof(StoreLocation), CertificateStoreLocation);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Unable to parse certificate store location: {CertificateStoreLocation}", ex);
            }

            var x509Store = new X509Store(storeName,
                                          storeLocation);

            x509Store.Open(OpenFlags.ReadOnly);

            X509Certificate2 x509Certificate;

            try
            {
                x509Certificate = x509Store.Certificates.Find(X509FindType.FindByThumbprint,
                                                              CertificateThumbprint,
                                                              validOnly: false)
                                                        .OfType<X509Certificate2>()
                                                        .Single();
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Unable to find certificate in cert:\\{CertificateStoreLocation}\\{CertificateStoreName} with thumbprint: {CertificateThumbprint}", ex);
            }

            var tokenCredential = new ClientCertificateCredential(
                                               TenantId,
                                                ClientId,
                                                x509Certificate);

            return tokenCredential;
        }
    }
}