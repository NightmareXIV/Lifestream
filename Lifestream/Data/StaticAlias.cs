using ECommons.Configuration;
using Newtonsoft.Json;

namespace Lifestream.Data;
public static class StaticAlias
{
    public static readonly CustomAlias GridaniaGC = JsonConvert.DeserializeObject<CustomAlias>("""
        {"ExportedName":"Gridania GC","Commands":[{"Kind":0,"Aetheryte":2,"Territory":132},{"Kind":5,"CenterPoint":{"X":32.913696,"Y":30.014404},"CircularExitPoint":{"X":22.40521,"Y":1.1999922,"Z":24.384962},"Clamp":{"Item1":3.0,"Item2":9.5},"Precision":7.0,"Tolerance":3,"WalkToExit":false,"Territory":132},{"Kind":1,"Point":{"X":22.895763,"Y":1.2310703,"Z":24.680761},"Scatter":0.5,"Territory":132},{"Kind":1,"Point":{"X":9.34315,"Y":-1.5100589,"Z":18.261602},"Scatter":2.0,"Territory":132},{"Kind":1,"Point":{"X":-23.782745,"Y":-4.4557166,"Z":12.125604},"Scatter":2.0,"Territory":132},{"Kind":1,"Point":{"X":-58.32065,"Y":-1.7947078,"Z":11.184064},"Scatter":1.0,"Territory":132},{"Kind":1,"Point":{"X":-61.23755,"Y":-0.5033426,"Z":7.463028},"Scatter":1.0,"Territory":132},{"Kind":1,"Point":{"X":-69.0996,"Y":-0.50220865,"Z":-5.859909},"Scatter":1.5,"Territory":132}]}
        """);

    public static readonly CustomAlias GridaniaGCC = JsonConvert.DeserializeObject<CustomAlias>("""
        {"ExportedName":"Gridania GCC","Commands":[{"Kind":0,"Aetheryte":2,"Territory":132},{"Kind":5,"CenterPoint":{"X":32.913696,"Y":30.014404},"CircularExitPoint":{"X":22.40521,"Y":1.1999922,"Z":24.384962},"Clamp":{"Item1":3.0,"Item2":9.5},"Precision":7.0,"Tolerance":3,"WalkToExit":false,"Territory":132},{"Kind":1,"Point":{"X":22.895763,"Y":1.2310703,"Z":24.680761},"Scatter":0.5,"Territory":132},{"Kind":1,"Point":{"X":9.34315,"Y":-1.5100589,"Z":18.261602},"Scatter":2.0,"Territory":132},{"Kind":1,"Point":{"X":-23.782745,"Y":-4.4557166,"Z":12.125604},"Scatter":2.0,"Territory":132},{"Kind":1,"Point":{"X":-58.32065,"Y":-1.7947078,"Z":11.184064},"Scatter":1.0,"Territory":132},{"Kind":1,"Point":{"X":-61.23755,"Y":-0.5033426,"Z":7.463028},"Scatter":1.0,"Territory":132},{"Kind":1,"Point":{"X":-78.66088,"Y":-0.5012527,"Z":-3.670311},"Scatter":0.6,"Territory":132}]}
        """);
    public static readonly CustomAlias GridaniaGCTicket = JsonConvert.DeserializeObject<CustomAlias>("""
        {"ExportedName":"Gridania GC - Ticket","Commands":[{"Kind":1,"Point":{"X":-61.23755,"Y":-0.5033426,"Z":7.463028},"Scatter":1.0,"Territory":132},{"Kind":1,"Point":{"X":-69.0996,"Y":-0.50220865,"Z":-5.859909},"Scatter":1.5,"Territory":132}]}
        """);

    public static readonly CustomAlias GridaniaGCCTicket = JsonConvert.DeserializeObject<CustomAlias>("""
        {"ExportedName":"Gridania GCC - Ticket","Commands":[{"Kind":1,"Point":{"X":-61.23755,"Y":-0.5033426,"Z":7.463028},"Scatter":1.0,"Territory":132},{"Kind":1,"Point":{"X":-78.66088,"Y":-0.5012527,"Z":-3.670311},"Scatter":0.6,"Territory":132}]}
        """);
    public static readonly CustomAlias UldahGC = JsonConvert.DeserializeObject<CustomAlias>("""
        {"ExportedName":"Uldah GC","Commands":[{"Kind":0,"Aetheryte":9,"Territory":130},{"Kind":5,"Point":{"X":50.086643,"Y":31.072668,"Z":-734.1901},"CenterPoint":{"X":-144.51825,"Y":-169.6651},"CircularExitPoint":{"X":-133.4609,"Y":-1.9999998,"Z":-157.48338},"Clamp":{"Item1":4.0,"Item2":5.4},"Precision":7.0,"Tolerance":3,"WalkToExit":false,"Territory":130},{"Kind":1,"Point":{"X":-132.53459,"Y":-2.0,"Z":-156.16699},"Scatter":1.0,"Territory":130},{"Kind":1,"Point":{"X":-120.153915,"Y":2.0565257,"Z":-140.8588},"WalkToExit":false,"Scatter":1.0,"Territory":130},{"Kind":1,"Point":{"X":-120.0627,"Y":2.0,"Z":-126.26132},"Scatter":1.0,"Territory":130},{"Kind":1,"Point":{"X":-117.30617,"Y":4.199996,"Z":-108.16911},"WalkToExit":false,"Scatter":0.2,"Territory":130},{"Kind":1,"Point":{"X":-123.37402,"Y":4.0999947,"Z":-98.41306},"WalkToExit":false,"Scatter":0.2,"Territory":130},{"Kind":1,"Point":{"X":-132.44452,"Y":4.0999994,"Z":-102.39349},"Scatter":0.5,"Territory":130},{"Kind":1,"Point":{"X":-142.6073,"Y":4.109976,"Z":-103.22401},"WalkToExit":false,"Scatter":1.3,"Territory":130}]}
        """);
    public static readonly CustomAlias UldahGCC = JsonConvert.DeserializeObject<CustomAlias>("""
        {"ExportedName":"Uldah GCC","Commands":[{"Kind":0,"Aetheryte":9,"Territory":130},{"Kind":5,"Point":{"X":50.086643,"Y":31.072668,"Z":-734.1901},"CenterPoint":{"X":-144.51825,"Y":-169.6651},"CircularExitPoint":{"X":-133.4609,"Y":-1.9999998,"Z":-157.48338},"Clamp":{"Item1":4.0,"Item2":5.4},"Precision":7.0,"Tolerance":3,"WalkToExit":false,"Territory":130},{"Kind":1,"Point":{"X":-132.53459,"Y":-2.0,"Z":-156.16699},"Scatter":1.0,"Territory":130},{"Kind":1,"Point":{"X":-120.153915,"Y":2.0565257,"Z":-140.8588},"WalkToExit":false,"Scatter":1.0,"Territory":130},{"Kind":1,"Point":{"X":-120.0627,"Y":2.0,"Z":-126.26132},"Scatter":1.0,"Territory":130},{"Kind":1,"Point":{"X":-117.30617,"Y":4.199996,"Z":-108.16911},"WalkToExit":false,"Scatter":0.2,"Territory":130},{"Kind":1,"Point":{"X":-123.37402,"Y":4.0999947,"Z":-98.41306},"WalkToExit":false,"Scatter":0.2,"Territory":130},{"Kind":1,"Point":{"X":-132.44452,"Y":4.0999994,"Z":-102.39349},"Scatter":0.5,"Territory":130},{"Kind":1,"Point":{"X":-151.35735,"Y":4.0999975,"Z":-94.71827},"WalkToExit":false,"Scatter":0.3,"Territory":130}]}
        """);
    public static readonly CustomAlias UldahGCTicket = JsonConvert.DeserializeObject<CustomAlias>("""
        {"ExportedName":"Uldah GC - Ticket","Commands":[{"Kind":1,"Point":{"X":-131.70963,"Y":4.0999956,"Z":-95.830505},"Scatter":0.5,"Territory":130},{"Kind":1,"Point":{"X":-142.6073,"Y":4.109976,"Z":-103.22401},"Scatter":1.3,"Territory":130}]}
        """);
    public static readonly CustomAlias UldahGCCTicket = JsonConvert.DeserializeObject<CustomAlias>("""
        {"ExportedName":"Uldah GCC - Ticket","Commands":[{"Kind":1,"Point":{"X":-131.70963,"Y":4.0999956,"Z":-95.830505},"Scatter":0.5,"Territory":130},{"Kind":1,"Point":{"X":-135.58601,"Y":4.0999975,"Z":-96.201675},"Scatter":0.5,"Territory":130},{"Kind":1,"Point":{"X":-151.35735,"Y":4.0999975,"Z":-94.71827},"Scatter":0.3,"Territory":130}]}
        """);
    public static readonly CustomAlias LimsaGC = JsonConvert.DeserializeObject<CustomAlias>("""
        {"ExportedName":"Limsa GC","Commands":[{"Kind":0,"Aetheryte":8},{"Kind":4,"Aetheryte":41},{"Kind":1,"Point":{"X":21.771105,"Y":40.000004,"Z":74.32061},"Scatter":0.8},{"Kind":1,"Point":{"X":62.326378,"Y":40.0,"Z":72.75588},"Scatter":1.0},{"Kind":1,"Point":{"X":92.38458,"Y":40.275368,"Z":72.14454},"Scatter":1.0}]}
        """);
    public static readonly CustomAlias LimsaGCC = JsonConvert.DeserializeObject<CustomAlias>("""
        {"ExportedName":"Limsa GCC","Commands":[{"Kind":0,"Aetheryte":8},{"Kind":4,"Aetheryte":41},{"Kind":1,"Point":{"X":21.771105,"Y":40.000004,"Z":74.32061},"Scatter":0.8},{"Kind":1,"Point":{"X":62.326378,"Y":40.0,"Z":72.75588},"Scatter":1.0},{"Kind":1,"Point":{"X":82.554245,"Y":40.246807,"Z":68.82362},"Scatter":1.0,"Territory":128},{"Kind":1,"Point":{"X":94.21555,"Y":40.247868,"Z":61.81552},"Scatter":0.3}]}
        """);
    public static readonly CustomAlias LimsaGCTicket = JsonConvert.DeserializeObject<CustomAlias>("""
        {"ExportedName":"Limsa GC - Ticket","Commands":[{"Kind":1,"Point":{"X":92.38458,"Y":40.275368,"Z":72.14454},"Scatter":1.0}]}
        """);
    public static readonly CustomAlias LimsaGCCTicket = JsonConvert.DeserializeObject<CustomAlias>("""
        {"ExportedName":"Limsa GCC - Ticket","Commands":[{"Kind":1,"Point":{"X":82.554245,"Y":40.246807,"Z":68.82362},"Scatter":1.0,"Territory":128},{"Kind":1,"Point":{"X":94.21555,"Y":40.247868,"Z":61.81552},"Scatter":0.3}]}
        """);

    public static readonly CustomAlias IslandSanctuary = JsonConvert.DeserializeObject<CustomAlias>("""{"ExportedName":"Island","Alias":"","Enabled":true,"Commands":[{"Kind":0,"Point":{"X":0.0,"Y":0.0,"Z":0.0},"Aetheryte":10,"World":0,"CenterPoint":{"X":0.0,"Y":0.0},"CircularExitPoint":{"X":0.0,"Y":0.0,"Z":0.0},"Clamp":null,"Precision":20.0,"Tolerance":1,"WalkToExit":true,"SkipTeleport":15.0},{"Kind":5,"Point":{"X":0.0,"Y":0.0,"Z":0.0},"Aetheryte":0,"World":0,"CenterPoint":{"X":156.11499,"Y":673.21277},"CircularExitPoint":{"X":169.34451,"Y":14.095893,"Z":670.086},"Clamp":null,"Precision":7.0,"Tolerance":3,"WalkToExit":true,"SkipTeleport":15.0}],"GUID":"00000000-0000-0000-0000-000000000000"}""");

    public static readonly CustomAlias CosmicExploration = JsonConvert.DeserializeObject<CustomAlias>("""
        {"ExportedName":"Cosmic","Alias":"cosmic","Enabled":false,"Commands":[{"Kind":0,"Aetheryte":175,"SkipTeleport":35.0},{"Kind":6,"DataID":175,"InteractDistance":10.0},{"Kind":9,"SelectOption":["<QuestDialogueText:transport/AetheryteBestwaysBurrow:2:Value>","<QuestDialogueText:transport/AetheryteBestwaysBurrow:1:Value>"],"StopOnScreenFade":true}]}
        """);

    public static readonly CustomAlias OccultCrescent = EzConfig.DefaultSerializationFactory.Deserialize<CustomAlias>("""
        {"ExportedName":"Occult Crescent","Alias":"OC","Enabled":false,"Commands":[{"Kind":0,"Aetheryte":216,"Precision":20.0,"Tolerance":1,"WalkToExit":true,"SkipTeleport":15.0},{"Kind":4,"Aetheryte":223,"Precision":20.0,"Tolerance":1,"WalkToExit":true,"SkipTeleport":15.0},{"Kind":1,"Point":{"X":193.60193,"Y":-17.964302,"Z":44.988438},"Precision":20.0,"Tolerance":1,"WalkToExit":true,"SkipTeleport":15.0},{"Kind":1,"Point":{"X":205.43477,"Y":-17.964302,"Z":53.52463},"Precision":20.0,"Tolerance":1,"WalkToExit":true,"SkipTeleport":15.0},{"Kind":1,"Point":{"X":206.27438,"Y":-17.9645,"Z":61.273403},"Precision":20.0,"Tolerance":1,"WalkToExit":true,"SkipTeleport":15.0},{"Kind":6,"Precision":20.0,"Tolerance":1,"WalkToExit":true,"SkipTeleport":15.0,"DataID":2014671},{"Kind":8,"Precision":20.0,"Tolerance":1,"WalkToExit":true,"SkipTeleport":15.0,"SelectOption":["<Warp:131594:Question>"],"StopOnScreenFade":true},{"Kind":1,"Point":{"X":-36.835308,"Y":0.0,"Z":-12.507464},"Precision":20.0,"Tolerance":1,"WalkToExit":true,"SkipTeleport":15.0},{"Kind":1,"Point":{"X":-74.80987,"Y":5.0,"Z":-15.057071},"Precision":20.0,"Tolerance":1,"WalkToExit":true,"SkipTeleport":15.0},{"Kind":6,"Precision":20.0,"Tolerance":1,"WalkToExit":true,"SkipTeleport":15.0,"DataID":1053611},{"Kind":9,"Precision":20.0,"Tolerance":1,"WalkToExit":true,"SkipTeleport":15.0,"SelectOption":["<QuestDialogueText:custom/009/CtsMkdEntrance_00929:1:Value>","「蜃気楼の島 クレセントアイル：南征編」に突入する"],"StopOnScreenFade":true},{"Kind":9,"Precision":20.0,"Tolerance":1,"WalkToExit":true,"SkipTeleport":15.0,"SelectOption":["<QuestDialogueText:custom/009/CtsMkdEntrance_00929:7:Value>"],"StopOnScreenFade":true},{"Kind":10,"Precision":20.0,"Tolerance":1,"WalkToExit":true,"SkipTeleport":15.0}],"GUID":"00000000-0000-0000-0000-000000000000"}
        """);

    public static readonly CustomAlias UldahMarketboard = JsonConvert.DeserializeObject<CustomAlias>("""
        {"ExportedName":"mbShortcut","Commands":[{"Kind":0,"Aetheryte":9},{"Kind":4,"Aetheryte":125},{"Kind":1,"Point":{"X":138.15443,"Y":4.0,"Z":-31.829441},"Scatter":0.7},{"Kind":1,"Point":{"X":145.9551,"Y":4.0,"Z":-32.124325},"Scatter":0.5},{"Kind":6,"DataID":2000442}]}
        """);

    public static readonly CustomAlias Firmament = JsonConvert.DeserializeObject<CustomAlias>("""
        {"ExportedName":"Firmament","Commands":[{"Kind":0,"Aetheryte":70},{"Kind":4,"Aetheryte":4294967295}]}
        """);
}
