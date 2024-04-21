using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CreamInstaller.Forms;
using static CreamInstaller.Resources.Resources;

namespace CreamInstaller.Resources;

internal static class CreamAPI
{
    internal static void GetCreamApiComponents(this string directory, out string api32, out string api32_o,
        out string api64, out string api64_o,
        out string config)
    {
        api32 = directory + @"\steam_api.dll";
        api32_o = directory + @"\steam_api_o.dll";
        api64 = directory + @"\steam_api64.dll";
        api64_o = directory + @"\steam_api64_o.dll";
        config = directory + @"\cream_api.ini";
        // TODO: account for log builds?
    }

    internal static void CheckConfig(string directory, Selection selection, InstallForm installForm = null)
    {
        // TODO
    }

    private static void WriteConfig(StreamWriter writer, string appId,
        SortedList<string, (string name, SortedList<string, SelectionDLC> injectDlc)> extraApps,
        SortedList<string, SelectionDLC> overrideDlc, SortedList<string, SelectionDLC> injectDlc,
        InstallForm installForm = null)
    {
        // TODO
    }

    internal static async Task Uninstall(string directory, InstallForm installForm = null, bool deleteOthers = true)
        => await Task.Run(() =>
        {
            // TODO
        });

    internal static async Task Install(string directory, Selection selection, InstallForm installForm = null,
        bool generateConfig = true)
        => await Task.Run(() =>
        {
            // TODO
        });

    internal static readonly Dictionary<ResourceIdentifier, HashSet<string>> ResourceMD5s = new()
    {
        [ResourceIdentifier.Steamworks32] =
        [
            "3F27FE562B778114D3943AC71DBFBC0A", // CreamAPI v2.0.0.3
            "E02D61C77F006DB758AD2107F5F50A5D", // CreamAPI v2.0.0.3 Hotfix
            "1E529F943F46721C8CBBBE815F21ABCC", // CreamAPI v2.0.0.4
            "1BD8B3C0D557E4E2738CFEF650F4CA59", // CreamAPI v2.0.0.5
            "3ED48A34F646252BE79EB76DF4F1BDAE", // CreamAPI v2.0.0.6
            "96C6DBD539043265AB148D907A4B5CA8", // CreamAPI v2.0.0.6 Hotfix
            "2807E591FE4B424F425F5DF0DFE48850", // CreamAPI v2.0.0.7 Log build
            "5875F0A87DB50910C9F0A41CC114697E", // CreamAPI v2.0.0.7 Non-log build
            "3F60789A82572953FEBEC4AA085BB003", // CreamAPI v3.0.0.0 Log build
            "2DC1D53403E6B3BE174FA14E9FEF9EBA", // CreamAPI v3.0.0.0 Non-log build
            "538445BC25A5D2B0BBE8CD174BCA819D", // CreamAPI v3.0.0.1 Log build
            "1F62652174623AA106992A63FD5957F0", // CreamAPI v3.0.0.1 Non-log build
            "09D3DDD8EB4257F334CEB37A7B1F9523", // CreamAPI v3.0.0.1 Hotfix Log build
            "DD1AE86BB1079788EFFA3DBC848841F6", // CreamAPI v3.0.0.1 Hotfix Non-log build
            "1D24F90D657A6EA8A573C9209C7A7AA6", // CreamAPI v3.0.0.2 Log build
            "49109F4468C4BEEA76AC0EF8177294AF", // CreamAPI v3.0.0.2 Non-log build
            "218FD1A013A8A48AF7277B66553A8442", // CreamAPI v3.0.0.3 Log build
            "332CB7444B5BA1A2972B4B888714AFA7", // CreamAPI v3.0.0.3 Non-log build
            "B4AD6FC46FE993FB4D902C56FC19D616", // CreamAPI v3.0.0.3 Hotfix Log build
            "837A65208B1C41CDA4298E6103D8B5A3", // CreamAPI v3.0.0.3 Hotfix Non-log build
            "048CC4E5560388FF6C0B2B0E7456314A", // CreamAPI v3.1.0.0 Log build
            "BB84A7B75272D4CE2A380EA37B1ABF75", // CreamAPI v3.1.0.0 Non-log build
            "9CECE8E9CB42E2266208BC8F8D430500", // CreamAPI v3.1.0.0 Hotfix Log build
            "CA541EDFFBBFD6A7CFB2156E098957C8", // CreamAPI v3.1.0.0 Hotfix Non-log build
            "23D0295523376845ED753251CE9F22D4", // CreamAPI v3.1.1.0 Log build
            "2C184424F2D2A7871A27826707C4349F", // CreamAPI v3.1.1.0 Non-log build
            "B6F6984BB4881CB59EA061E6F3B57B17", // CreamAPI v3.2.0.0 Log build
            "FE4328E3C531A36C7A5A6D94B8DF8291", // CreamAPI v3.2.0.0 Non-log build
            "ADBA3AD528DF352708C3B6C275F5981C", // CreamAPI v3.3.0.0 Log build
            "44155549BBE68B89D3D1DE351C05AC59", // CreamAPI v3.3.0.0 Non-log build
            "7F1E2B10589D0082965DA4208930227C", // CreamAPI v3.4.0.0 Log build
            "A4B47C35963D001D32A7D03EBD9F3453", // CreamAPI v3.4.0.0 Non-log build
            "1652704A2526470475A8D8F69D48D0C8", // CreamAPI v3.4.1.0 Log build
            "6A796C90DF769BBD9028D75B00724394", // CreamAPI v3.4.1.0 Non-log build
            "CF21E86A249AC4FFF95CF4A7C2ED4AB9", // CreamAPI v3.4.1.0 Unprotected Log build
            "2E240FAD41B63F9D40974861F62284E8", // CreamAPI v3.4.1.0 Unprotected Non-log build
            "AF918ED50587908743FE012A5F73F6ED", // CreamAPI v4.0.0.0 Log build
            "07BF125E247C2EB9CC6C3847251F4F49", // CreamAPI v4.0.0.0 Non-log build
            "7B23BCCD9B40163B1E03FCDD0E1F0ED3", // CreamAPI v4.1.0.0 Log build
            "E5313C88EB82E6B4C4C616B45BD6DCC7", // CreamAPI v4.1.0.0 Non-log build
            "771B2365B50D94D30078C199F40A3907", // CreamAPI v4.1.1.0 Log build
            "AABD994DA9317A26162B4077D647A699", // CreamAPI v4.1.1.0 Non-log build
            "009528A262505DCD6C2D2F02619D78A8", // CreamAPI v4.1.1.0 Hotfix Log build
            "C6F267A2D10B891ED352ED849A28D69B", // CreamAPI v4.1.1.0 Hotfix Non-log build
            "5CDA107708E4A646B88DFC5BDDF1541B", // CreamAPI v4.2.0.0 Log build
            "2F52261B9ED11CE504A1A2E1E488441C", // CreamAPI v4.2.0.0 Non-log build
            "B458F3D9F81135C2F63882A6231B4D8E", // CreamAPI v4.2.1.0 Log build
            "DEDD1461B8ED4A57D01F9CFC2458B24E", // CreamAPI v4.2.1.0 Non-log build
            "5A1631407B8AC5FBC650206BDA074C75", // CreamAPI v4.3.0.0 Log build
            "D085948513D6149EC8BC009C00DCDC7F", // CreamAPI v4.3.0.0 Non-log build
            "6E6F41FEC249E18FA2A24829F07BBCAE", // CreamAPI v4.3.1.0 Log build
            "17EA360D51868FED90FA4024F8C25E2F", // CreamAPI v4.3.1.0 Non-log build
            "C09447B04554CB80757652853C44351F", // CreamAPI v4.4.0.0 Log build
            "3875FE47DA334BFC3454C51AACE37E6E", // CreamAPI v4.4.0.0 Non-log build
            "624C88B6C4EE9DFE2844DE41B8F92378", // CreamAPI v4.5.0.0 Log build
            "ACE12F5B69D961F814BD2CE5B38150C3", // CreamAPI v4.5.0.0 Non-log build
            "8CD5E2A20FBEF3320053B3CAFA23F140", // CreamAPI v4.5.0.0 Hotfix Log build
            "02594110FE56B2945955D46670B9A094", // CreamAPI v4.5.0.0 Hotfix Non-log build
            "23909B4B1C7A182A6596BD0FDF2BFC7C", // CreamAPI v5.0.0.0 Log build
            "E6DDF91F4419BE471FBE126A0966648B", // CreamAPI v5.0.0.0 Non-log build
            "B14007170E59B03D5DF844BD3457295B", // CreamAPI v5.1.0.0 Log build
            "24C712826D939F5CEC9049D4B94FCBDB" // CreamAPI v5.1.0.0 Non-log build
        ],
        [ResourceIdentifier.Steamworks64] =
        [
            "07C4B41397E4281F7C5996510726C02E", // CreamAPI v2.0.0.3
            "B48D06E6F49CF076CDE46D1B432FFBDB", // CreamAPI v2.0.0.3 Hotfix
            "A91F9B5DE942E475597F6B02FAD3F737", // CreamAPI v2.0.0.4
            "4D768E2E52D26B7C23858B9D7BE884D4", // CreamAPI v2.0.0.5
            "ABAB50B83E81B2ABCC281D10F32E1CF5", // CreamAPI v2.0.0.6
            "1FFE3EA34F8FEB126F807835197B8200", // CreamAPI v2.0.0.6 Hotfix
            "0699EA1251E153E5B90A18EA3F194FDB", // CreamAPI v2.0.0.7 Log build
            "D868939DCC632DCC15CF1D04521AAA1E", // CreamAPI v2.0.0.7 Non-log build
            "6DD978DF2EBB1B80A49DFBEBFC73BB90", // CreamAPI v3.0.0.0 Log build
            "ECCEC92FD00020A6E01A5E498F94C996", // CreamAPI v3.0.0.0 Non-log build
            "E23953FFDB7AEEA684B01C51C8DC3267", // CreamAPI v3.0.0.1 Log build
            "0B69D4BCF11C8E93EABDC18F37F9C09E", // CreamAPI v3.0.0.1 Non-log build
            "12CBDAAF40984C4A40A73367B75AF278", // CreamAPI v3.0.0.1 Hotfix Log build
            "355F9D047D3423CB6B14131602BDAF90", // CreamAPI v3.0.0.1 Hotfix Non-log build
            "DD68C342001AAAF4CDF5AEF6D20EBB5C", // CreamAPI v3.0.0.2 Log build
            "F2231102C3E2F71EF680461C305EB192", // CreamAPI v3.0.0.2 Non-log build
            "AD36A2FBD962823D5D3E27D943DF472E", // CreamAPI v3.0.0.3 Log build
            "F9C6F0CCFC4CCF17F8C648E61AFFB950", // CreamAPI v3.0.0.3 Non-log build
            "1A8B72727C64B106F6457D26F121CE4F", // CreamAPI v3.0.0.3 Hotfix Log build
            "41D2A5AC42A408E24B9E1232DCCF9ABB", // CreamAPI v3.0.0.3 Hotfix Non-log build
            "B750AC84B4A2214F994F1BE98C9B240D", // CreamAPI v3.1.0.0 Log build
            "2235A6FB34CEB62A654D5BF8FC53ADD9", // CreamAPI v3.1.0.0 Non-log build
            "E2FEE639D36A9AD35165B430ABF6205B", // CreamAPI v3.1.0.0 Hotfix Log build
            "52262CCD9AFC64BB1F8AAFD81CD3DF1B", // CreamAPI v3.1.0.0 Hotfix Non-log build
            "04D07B6E9415E8FE0A1A83742F635703", // CreamAPI v3.1.1.0 Log build
            "478D299E756B5B9DB1269D39163A89FA", // CreamAPI v3.1.1.0 Non-log build
            "443BC83F73574801AA5F58B3176E9D13", // CreamAPI v3.2.0.0 Log build
            "B72662E67115C0BD5B71B0F0C25426DB", // CreamAPI v3.2.0.0 Non-log build
            "D6B381A977235F5255CCD894C831751B", // CreamAPI v3.3.0.0 Log build
            "7C25B1239BDF8E1C2EACD84F90191C20", // CreamAPI v3.3.0.0 Non-log build
            "766013201E35025167E98360997C61AD", // CreamAPI v3.4.0.0 Log build
            "8C042DA51EE6B208849AB79CF12CED1C", // CreamAPI v3.4.0.0 Non-log build
            "F65DFA7ED9D45EE0DB348DA48A7704C6", // CreamAPI v3.4.1.0 Log build
            "D3C537DF603C6A1889552161306C3249", // CreamAPI v3.4.1.0 Non-log build
            "D429DBF28E6B44B8ED31185E6329243C", // CreamAPI v3.4.1.0 Unprotected Log build
            "F0A84666D016827715EAB1177967D337", // CreamAPI v3.4.1.0 Unprotected Non-log build
            "3BA9ECDDF3A4A80EA823071E7F07C9A6", // CreamAPI v4.0.0.0 Log build
            "924335929155BA8F820765F599221FA7", // CreamAPI v4.0.0.0 Non-log build
            "7BF03DFBB1806A78EC6A6D35CA914AC9", // CreamAPI v4.1.0.0 Log build
            "A17AA2D3ED990D1FF3EF724935439EBA", // CreamAPI v4.1.0.0 Non-log build
            "B8A3BB90C3628B52267BB9F8F46FBDC6", // CreamAPI v4.1.1.0 Log build
            "550170600EC33762A1090B22939A1C0E", // CreamAPI v4.1.1.0 Non-log build
            "3142D8B9255B017A95CCA1A6292E2482", // CreamAPI v4.1.1.0 Hotfix Log build
            "645C728A6117946294130D07CF6C0CAE", // CreamAPI v4.1.1.0 Hotfix Non-log build
            "8FA88A5EEEB53899055D4A79C21FE021", // CreamAPI v4.2.0.0 Log build
            "0D754EF9C26A4D3EB3633580AE97E399", // CreamAPI v4.2.0.0 Non-log build
            "EEFA867EDB29CEED3820E0B6F5D0F976", // CreamAPI v4.2.1.0 Log build
            "59CF18A38D83D5479B07B4BDF021BAD1", // CreamAPI v4.2.1.0 Non-log build
            "02B08A93D6911C462CE62AD9941D4EF1", // CreamAPI v4.3.0.0 Log build
            "0511C45A1BDF64B00147F96F7F67E167", // CreamAPI v4.3.0.0 Non-log build
            "12CE469EF5B5AE02318B067B42D551DE", // CreamAPI v4.3.1.0 Log build
            "56FC360E9F80986AD3CE754F134D4420", // CreamAPI v4.3.1.0 Non-log build
            "CBABB2599922DC8C04AB8D3B00A9560D", // CreamAPI v4.4.0.0 Log build
            "9FF5374F639ABA21EC77932B0B572697", // CreamAPI v4.4.0.0 Non-log build
            "8EA47C4D41BAE7601FFC996B91E741FD", // CreamAPI v4.5.0.0 Log build
            "50D2291A77595DD5CA187ED5F7EC1286", // CreamAPI v4.5.0.0 Non-log build
            "342B0380D6E12DF8EF1FC5588DCC1167", // CreamAPI v4.5.0.0 Hotfix Log build
            "30091B91923D9583A54A93ED1145554B", // CreamAPI v4.5.0.0 Hotfix Non-log build
            "15D76C0CBB175AA94936200C5208611E", // CreamAPI v5.0.0.0 Log build
            "B7CF4BC4020C6419249E32EE126FF647", // CreamAPI v5.0.0.0 Non-log build
            "BE635705410B93A1075ED32AA97E3B5C", // CreamAPI v5.1.0.0 Log build
            "1B14C913C0DF41CC0667993D9B37404D" // CreamAPI v5.1.0.0 Non-log build
        ]
    };
}