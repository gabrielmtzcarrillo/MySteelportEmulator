using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace SaintsRowAPI
{
    public sealed class CertificateLoadException : Exception
    {
        public CertificateLoadException(string message)
            : base(message)
        {
        }

        public CertificateLoadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    public static class Certificates
    {
        private const string PfxPassword = "temp";

        public static X509Certificate2 Certificate { get; private set; }
        public static Org.BouncyCastle.X509.X509Certificate BcCertificate { get; private set; }
        public static AsymmetricKeyParameter BcPrivateKey { get; private set; }

        private static string Pfx = @"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";

        public static void Load()
        {
            byte[] pfxBytes = LoadEmbeddedPfxBytes();

            Certificate = LoadRuntimeCertificate(pfxBytes);
            if (!Certificate.HasPrivateKey)
                throw new CertificateLoadException("Embedded PKCS#12 loaded, but the X509 certificate does not contain a private key.");

            LoadBouncyCastleCertificateAndKey(pfxBytes);

            Console.WriteLine("Certificate loaded. Subject: {0}", Certificate.Subject);
            Console.WriteLine("Has Private Key: {0}", Certificate.HasPrivateKey);
        }

        private static byte[] LoadEmbeddedPfxBytes()
        {
            try
            {
                return Convert.FromBase64String(Pfx);
            }
            catch (FormatException ex)
            {
                throw new CertificateLoadException("Embedded PKCS#12 payload is not valid base64.", ex);
            }
        }

        private static X509Certificate2 LoadRuntimeCertificate(byte[] pfxBytes)
        {
            try
            {
                return X509CertificateLoader.LoadPkcs12(pfxBytes, PfxPassword);
            }
            catch (Exception ex)
            {
                throw new CertificateLoadException("Embedded PKCS#12 could not be loaded by X509CertificateLoader. Verify the PFX bytes and password.", ex);
            }
        }

        private static void LoadBouncyCastleCertificateAndKey(byte[] pfxBytes)
        {
            Org.BouncyCastle.Pkcs.Pkcs12Store pkcs12;

            try
            {
                pkcs12 = new Org.BouncyCastle.Pkcs.Pkcs12StoreBuilder().Build();
                using (var ms = new MemoryStream(pfxBytes))
                {
                    pkcs12.Load(ms, PfxPassword.ToCharArray());
                }
            }
            catch (Exception ex)
            {
                throw new CertificateLoadException("Embedded PKCS#12 could not be parsed by Bouncy Castle. TLS cannot start without this certificate/key pair.", ex);
            }

            string alias = pkcs12.Aliases.Cast<string>().FirstOrDefault(a => pkcs12.IsKeyEntry(a));
            if (alias == null)
                throw new CertificateLoadException("Embedded PKCS#12 does not contain a private-key entry.");

            Org.BouncyCastle.Pkcs.X509CertificateEntry certificateEntry = pkcs12.GetCertificate(alias);
            if (certificateEntry == null || certificateEntry.Certificate == null)
                throw new CertificateLoadException(String.Format("Embedded PKCS#12 private-key entry '{0}' does not include a certificate.", alias));

            Org.BouncyCastle.Pkcs.AsymmetricKeyEntry keyEntry = pkcs12.GetKey(alias);
            if (keyEntry == null || keyEntry.Key == null)
                throw new CertificateLoadException(String.Format("Embedded PKCS#12 private-key entry '{0}' could not be loaded.", alias));

            if (!keyEntry.Key.IsPrivate)
                throw new CertificateLoadException(String.Format("Embedded PKCS#12 key entry '{0}' is not a private key.", alias));

            BcCertificate = certificateEntry.Certificate;
            BcPrivateKey = keyEntry.Key;

            if (BcCertificate == null)
                throw new CertificateLoadException("Bouncy Castle certificate was not loaded from the embedded PKCS#12 payload.");

            if (BcPrivateKey == null)
                throw new CertificateLoadException("Bouncy Castle private key was not loaded from the embedded PKCS#12 payload.");
        }
    }
}
