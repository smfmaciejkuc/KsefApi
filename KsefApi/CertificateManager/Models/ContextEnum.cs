using System.Runtime.Serialization;

namespace CertificateManager.Models.QRCode
{
    public enum QRCodeContextIdentifierType
    {
        [EnumMember(Value = "Nip")]
        Nip,
        [EnumMember(Value = "InternalId")]
        InternalId,
        [EnumMember(Value = "NipVatUe")]
        NipVatUe
    }
}
