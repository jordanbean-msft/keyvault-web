using System.Security.Cryptography.X509Certificates;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

if (bool.Parse(builder.Configuration["IsHostedInAzure"]))
{
  builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{builder.Configuration["Authentication:KeyVaultName"]}.vault.azure.net/"),
    new DefaultAzureCredential(new DefaultAzureCredentialOptions
    {
      ManagedIdentityClientId = builder.Configuration["Authentication:ManagedIdentityClientId"]
    }));
}
else
{
  var x509Store = new X509Store(StoreName.My,
                                StoreLocation.CurrentUser);

  x509Store.Open(OpenFlags.ReadOnly);

  var x509Certificate = x509Store.Certificates.Find(X509FindType.FindByThumbprint,
                                                    builder.Configuration["Authentication:AzureADCertificateThumbprint"],
                                                    validOnly: false)
                                              .OfType<X509Certificate2>()
                                              .Single();

  builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{builder.Configuration["Authentication:KeyVaultName"]}.vault.azure.net/"),
    new ClientCertificateCredential(
      builder.Configuration["Authentication:AzureADDirectoryId"],
      builder.Configuration["Authentication:AzureADApplicationId"],
      x509Certificate));
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
