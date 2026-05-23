using System;
using System.Collections.Generic;
using System.IO;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace SaintsRowAPI.Hydra
{
    public class HydraTlsServer : DefaultTlsServer
    {
        private readonly Org.BouncyCastle.X509.X509Certificate _cert;
        private readonly AsymmetricKeyParameter _privateKey;

        public HydraTlsServer(Org.BouncyCastle.X509.X509Certificate cert, AsymmetricKeyParameter privateKey)
        {
            _cert = cert;
            _privateKey = privateKey;
        }

        public override TlsCredentials GetCredentials()
        {
            return new DefaultTlsSignerCredentials(mContext, new Certificate(new[] { _cert.CertificateStructure }), _privateKey);
        }

        protected override ProtocolVersion MaximumVersion => ProtocolVersion.TLSv12;
        protected override ProtocolVersion MinimumVersion => ProtocolVersion.TLSv10;

        protected override int[] GetCipherSuites()
        {
            // Specifically include cipher suites likely used by legacy clients
            return new int[] {
                CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384,
                CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256,
                CipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA,
                CipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA,
                CipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA
            };
        }
    }
}
