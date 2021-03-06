﻿using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.X509;
using System;
using System.IO;

namespace SharpOCSP
{
    public class SoftToken : IToken
    {
		public  override String Name{ get ; protected set; }
		private X509Certificate _ocspCertificate;
		private RsaPrivateCrtKeyParameters _privateKey;
        /// <summary>
        /// Creates an instance of a 'SoftToken'.
        /// </summary>
        /// <param name="name">The token's name.</param>
        /// <param name="certPath">Path to OCSP signer certificate.</param>
        /// <param name="keyPath">Path to OCSP signer certificate key.</param>
		public override byte[] SignData(byte[] data, IDigest digestAlgorithm)
		{
			byte[] signature;
			RsaDigestSigner rsaSigner = new RsaDigestSigner(digestAlgorithm);

			rsaSigner.Init(true, _privateKey);
			rsaSigner.BlockUpdate(data, 0, data.Length);
			signature = rsaSigner.GenerateSignature();
			rsaSigner.Reset();

			return signature;
		}
		public override AsymmetricKeyParameter GetPublicKey ()
		{
			return _ocspCertificate.GetPublicKey ();
		}
		public override AsymmetricKeyParameter GetPrivateKey ()
		{
			return  (AsymmetricKeyParameter)_privateKey;
		}
		public override X509Certificate GetOcspSigningCert()
		{
			return _ocspCertificate;
		}
        public SoftToken(string name,string certPath, string keyPath)
        {
            Name = name;
			SharpOCSP.log.Debug ("Configuring token: " + name);
			SharpOCSP.log.Debug ("Certificate path: " + certPath);
            //Read OCSP signer certificate
			try{
	            var ocspCertReader = new PemReader(new StreamReader(certPath));
				_ocspCertificate = (X509Certificate) ocspCertReader.ReadObject();
			}catch (System.UnauthorizedAccessException e){
				throw new OcspFilesystemException ("Error reading ocsp certificate: " + keyPath, e);
			}catch (FileNotFoundException e){
				throw new OcspFilesystemException ("Error reading ocsp certificate: " + keyPath, e);
			}
			SharpOCSP.log.Debug ("Certificate key path: " + keyPath);
            //Read private key
			try{
            	var keyReader = new PemReader(new StreamReader(keyPath));
				var key_from_pem =  keyReader.ReadObject ();
				RsaPrivateCrtKeyParameters private_key = key_from_pem as RsaPrivateCrtKeyParameters;
				AsymmetricCipherKeyPair private_key_pair = key_from_pem as AsymmetricCipherKeyPair;
				if ( private_key != null){
					_privateKey = private_key;
				}else if (private_key_pair != null){
					_privateKey = (RsaPrivateCrtKeyParameters)private_key_pair.Private;
				}
				else{
					throw new OcspFilesystemException("Error reading private key: " + keyPath);
				}
			}catch (System.UnauthorizedAccessException e){
				throw new OcspFilesystemException ("Can't access private key path: " + keyPath, e);
			}catch (FileNotFoundException e){
				throw new OcspFilesystemException ("PEM file doesn't exist: " + keyPath, e);
			}catch (InvalidCastException e){
				throw new OcspFilesystemException ("Is it PEM really?: " + keyPath, e);
			}
        }
    }
}
