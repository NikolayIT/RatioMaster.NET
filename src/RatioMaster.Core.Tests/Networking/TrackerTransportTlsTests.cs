using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using RatioMaster.Core.Networking;
using RatioMaster.Core.Tests.Fakes;

namespace RatioMaster.Core.Tests.Networking;

public class TrackerTransportTlsTests
{
    private static CancellationToken Timeout => new CancellationTokenSource(TimeSpan.FromSeconds(20)).Token;

    [Fact]
    public async Task WrapsConnectionInTlsWhenCertificateErrorsAreIgnored()
    {
        using var certificate = CreateSelfSignedCertificate();
        await using var server = LoopbackServer.Start(async (stream, ct) =>
        {
            await using var ssl = new SslStream(stream, leaveInnerStreamOpen: false);
            await ssl.AuthenticateAsServerAsync(certificate, clientCertificateRequired: false, checkCertificateRevocation: false);
            var request = await LoopbackServer.ReadExactAsync(ssl, 5, ct);
            await ssl.WriteAsync(request, ct);
        });

        await using var stream = await TrackerTransport.Instance.ConnectAsync(
            "127.0.0.1", server.Port, useTls: true, ignoreCertificateErrors: true, ProxySettings.None, Timeout);

        await stream.WriteAsync("hello"u8.ToArray(), Timeout);
        var echoed = await LoopbackServer.ReadExactAsync(stream, 5, Timeout);
        Assert.Equal("hello", Encoding.ASCII.GetString(echoed));
    }

    [Fact]
    public async Task RejectsUntrustedCertificateByDefault()
    {
        using var certificate = CreateSelfSignedCertificate();
        await using var server = LoopbackServer.Start(async (stream, ct) =>
        {
            await using var ssl = new SslStream(stream, leaveInnerStreamOpen: false);
            try
            {
                await ssl.AuthenticateAsServerAsync(certificate, clientCertificateRequired: false, checkCertificateRevocation: false);
            }
            catch (Exception)
            {
                // The client aborts the handshake; ignore on the server side.
            }
        });

        await Assert.ThrowsAsync<AuthenticationException>(async () =>
            await TrackerTransport.Instance.ConnectAsync(
                "127.0.0.1", server.Port, useTls: true, ignoreCertificateErrors: false, ProxySettings.None, Timeout));
    }

    private static X509Certificate2 CreateSelfSignedCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var ephemeral = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        // Round-trip through PFX so the private key is usable for server authentication on Windows.
        return X509CertificateLoader.LoadPkcs12(ephemeral.Export(X509ContentType.Pfx), null);
    }
}
