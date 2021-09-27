using SlowCryptoLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CryptoHelpers
{
    public class CryptoHelper : ICryptoHelper
    {
        private readonly IStore store;
        private Dictionary<ICertificateParam, ICertificate> paramsAndCerts;
        private Dictionary<string, ICertificate> stringAndCerts;
        private IEnumerator<ICertificate> certificates;
        private IEnumerator<ICertificateParam> parameters;
        private ICertificate cert;
        private ICertificateParam parametr;
        private Dictionary<ICertificate, int> list;

        public CryptoHelper(IStore store)
        {
            this.store = store;
            paramsAndCerts = new Dictionary<ICertificateParam, ICertificate>();
            stringAndCerts = new Dictionary<string, ICertificate>();
            certificates = store.Certificates.GetEnumerator();
            certificates.MoveNext();
            cert = certificates.Current;
            parameters = cert.CertificateParams.GetEnumerator();
            list = new Dictionary<ICertificate, int>();
        }

        public byte[] Sign(byte[] data, string certParamValue)
        {
            return FindCertificate(certParamValue).Sign(data);
        }

        public bool Verify(byte[] signature, string certParamValue)
        {
            return FindCertificate(certParamValue).Verify(signature);
        }

        private ICertificate FindCertificate(string certParamValue)
        {
            if (stringAndCerts.ContainsKey(certParamValue))
                return stringAndCerts[certParamValue];
            for (int i = 0; i < paramsAndCerts.Count(); i++)
            {
                var param = paramsAndCerts.ElementAt(i);
                if (param.Key.Is(certParamValue))
                {
                    stringAndCerts[certParamValue] = param.Value;
                    paramsAndCerts.Remove(param.Key);
                    return stringAndCerts[certParamValue];
                }
            }
            return FindCertificateInStore(certParamValue);
        }
        private bool wasException = false;

        private ICertificate FindCertificateInStore(string certParamValue)
        {
            try
            {
                if (wasException)
                {
                    for (int i = 0; i < list.Count(); i++)
                    {
                        certificates.MoveNext();
                    }
                    cert = certificates.Current;
                    parameters = cert.CertificateParams.GetEnumerator();
                    for (int i = 0; i < list[cert]; i++)
                    {
                        parameters.MoveNext();
                    }
                    wasException = false;
                }
                while (true)
                {
                    if (!list.ContainsKey(cert)) list[cert] = 0;
                    while (parameters.MoveNext())
                    {
                        list[cert]++;
                        parametr = parameters.Current;
                        if (parametr.Is(certParamValue))
                        {
                            stringAndCerts[certParamValue] = cert;
                            return cert;
                        }
                        paramsAndCerts[parametr] = cert;
                    }
                    if (certificates.MoveNext())
                    {
                        cert = certificates.Current;
                        parameters = cert.CertificateParams.GetEnumerator();
                    }
                    else break;
                }
                stringAndCerts[certParamValue] = null;
                throw new Exception();
            }
            catch(Exception e)
            {
                certificates = store.Certificates.GetEnumerator();
                wasException = true;
                throw e;
            }
        }

        public void Dispose()
        {
            cert = null;
            parametr = null;
            foreach(var c in list)
            {
                c.Key.Dispose();
            }
            parameters.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}