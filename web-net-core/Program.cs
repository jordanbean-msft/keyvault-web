using System.Security.Cryptography.X509Certificates;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

string kvUri = $"https://{builder.Configuration["KeyVaultName"]}.vault.azure.net/";

if (bool.Parse(builder.Configuration["IsHostedOnPrem"]))
{
  var x509Store = new X509Store(StoreName.My,
                                StoreLocation.LocalMachine);

  x509Store.Open(OpenFlags.ReadOnly);

  X509Certificate2 x509Certificate;

  try
  {
    x509Certificate = x509Store.Certificates.Find(X509FindType.FindByThumbprint,
                                                  builder.Configuration["Authentication:AzureADCertificateThumbprint"],
                                                  validOnly: false)
                                            .OfType<X509Certificate2>()
                                            .Single();
  }
  catch (Exception ex)
  {
    throw new Exception($"Unable to find certificate in cert:\\LocalMachine\\My with thumbprint: {builder.Configuration["Authentication:AzureADCertificateThumbprint"]}", ex);
  }

  try
  {
    builder.Configuration.AddAzureKeyVault(new Uri(kvUri), new ClientCertificateCredential(
      builder.Configuration["Authentication:AzureADDirectoryId"],
      builder.Configuration["Authentication:AzureADApplicationId"],
      x509Certificate));
  }
  catch (Exception ex)
  {
    throw new Exception($"Unable to create SecretClient with Uri: {kvUri}, AzureADDirectoryId: {builder.Configuration["Authentication:AzureADDirectoryId"]}, AzureADApplicationId: {builder.Configuration["Authentication:AzureADApplicationId"]}, certificateThumbprint: {builder.Configuration["Authentication:AzureADCertificateThumbprint"]}", ex);
  }
}
else
{
  builder.Configuration.AddAzureKeyVault(new Uri(kvUri), new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = builder.Configuration["Authentication:ManagedIdentityClientId"] }));
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Home/Error");
  // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
  app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
