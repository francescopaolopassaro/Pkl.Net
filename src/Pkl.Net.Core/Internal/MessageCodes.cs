// Pkl.Net - Passaro Francesco Paolo 2026
namespace PklNet.Core.Internal;

internal static class MessageCodes
{
    // Protocol message codes (server <-> client)
    internal const int CreateEvaluator              = 0x20;
    internal const int CreateEvaluatorResponse      = 0x21;
    internal const int CloseEvaluator               = 0x22;
    internal const int Evaluate                     = 0x23;
    internal const int EvaluateResponse             = 0x24;
    internal const int EvaluateLog                  = 0x25;
    internal const int EvaluateRead                 = 0x26;
    internal const int EvaluateReadResponse         = 0x27;
    internal const int EvaluateReadModule           = 0x28;
    internal const int EvaluateReadModuleResponse   = 0x29;
    internal const int ListResourcesRequest         = 0x2a;
    internal const int ListResourcesResponse        = 0x2b;
    internal const int ListModulesRequest           = 0x2c;
    internal const int ListModulesResponse          = 0x2d;
    internal const int InitializeModuleReaderRequest    = 0x2e;
    internal const int InitializeModuleReaderResponse   = 0x2f;
    internal const int InitializeResourceReaderRequest  = 0x30;
    internal const int InitializeResourceReaderResponse = 0x31;
    internal const int CloseExternalProcess         = 0x32;

    // Pkl value type codes (inside evaluation results)
    internal const int PklObject    = 0x01;
    internal const int PklMap       = 0x02;
    internal const int PklMapping   = 0x03;
    internal const int PklList      = 0x04;
    internal const int PklListing   = 0x05;
    internal const int PklSet       = 0x06;
    internal const int PklDuration  = 0x07;
    internal const int PklDataSize  = 0x08;
    internal const int PklPair      = 0x09;
    internal const int PklIntSeq    = 0x0A;
    internal const int PklRegex     = 0x0B;
    internal const int PklClass     = 0x0C;
    internal const int PklTypeAlias = 0x0D;
    internal const int PklFunction  = 0x0E;
    internal const int PklBytes     = 0x0F;

    // Object member codes
    internal const int MemberProperty = 0x10;
    internal const int MemberEntry    = 0x11;
    internal const int MemberElement  = 0x12;
}
