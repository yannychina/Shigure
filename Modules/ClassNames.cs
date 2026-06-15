namespace Shigure;

public static class ClassNames
{
    private static readonly Dictionary<int, string> Classes = new()
    {
        [1] = "战士",
        [2] = "圣骑士",
        [3] = "猎人",
        [4] = "盗贼",
        [5] = "牧师",
        [6] = "死亡骑士",
        [7] = "萨满",
        [8] = "法师",
        [9] = "术士",
        [10] = "武僧",
        [11] = "德鲁伊",
        [12] = "恶魔猎手",
        [13] = "唤魔师"
    };

    private static readonly Dictionary<(int ClassId, int SpecId), string> Specs = new()
    {
        [(1, 1)] = "武器",
        [(1, 2)] = "狂怒",
        [(1, 3)] = "防护",
        [(2, 1)] = "神圣",
        [(2, 2)] = "防护",
        [(2, 3)] = "惩戒",
        [(3, 1)] = "兽王",
        [(3, 2)] = "射击",
        [(3, 3)] = "生存",
        [(4, 1)] = "刺杀",
        [(4, 2)] = "狂徒",
        [(4, 3)] = "敏锐",
        [(5, 1)] = "戒律",
        [(5, 2)] = "神牧",
        [(5, 3)] = "暗影",
        [(6, 1)] = "鲜血",
        [(6, 2)] = "冰霜",
        [(6, 3)] = "邪恶",
        [(7, 1)] = "元素",
        [(7, 2)] = "增强",
        [(7, 3)] = "奶萨",
        [(8, 1)] = "奥术",
        [(8, 2)] = "火焰",
        [(8, 3)] = "冰霜",
        [(9, 1)] = "痛苦",
        [(9, 2)] = "恶魔",
        [(9, 3)] = "毁灭",
        [(10, 1)] = "酒仙",
        [(10, 2)] = "织雾",
        [(10, 3)] = "踏风",
        [(11, 1)] = "平衡",
        [(11, 2)] = "野性",
        [(11, 3)] = "守护",
        [(11, 4)] = "奶德",
        [(12, 1)] = "浩劫",
        [(12, 2)] = "复仇",
        [(12, 3)] = "噬灭",
        [(13, 1)] = "湮灭",
        [(13, 2)] = "恩护",
        [(13, 3)] = "增辉"
    };

    private static readonly Dictionary<(int ClassId, int SpecId, int HeroTalentId), string> HeroTalents = new()
    {
        [(1, 1, 1)] = "巨神兵",
        [(1, 1, 2)] = "屠戮者",
        [(1, 2, 2)] = "屠戮者",
        [(1, 2, 3)] = "山丘领主",
        [(1, 3, 1)] = "巨神兵",
        [(1, 3, 3)] = "山丘领主",

        [(2, 1, 1)] = "烈日先驱",
        [(2, 1, 2)] = "铸光者",
        [(2, 2, 1)] = "铸光者",
        [(2, 2, 3)] = "圣殿骑士",
        [(2, 3, 1)] = "烈日先驱",
        [(2, 3, 3)] = "圣殿骑士",

        [(3, 1, 1)] = "黑暗游侠",
        [(3, 1, 2)] = "猎群领袖",
        [(3, 2, 1)] = "黑暗游侠",
        [(3, 2, 3)] = "哨兵",
        [(3, 3, 1)] = "猎群领袖",
        [(3, 3, 3)] = "哨兵",

        [(4, 1, 1)] = "死亡猎手",
        [(4, 1, 2)] = "命缚者",
        [(4, 2, 1)] = "命缚者",
        [(4, 2, 3)] = "欺诈者",
        [(4, 3, 1)] = "死亡猎手",
        [(4, 3, 3)] = "欺诈者",

        [(5, 1, 1)] = "神谕者",
        [(5, 1, 2)] = "虚空编织者",
        [(5, 2, 1)] = "神谕者",
        [(5, 2, 3)] = "执政官",
        [(5, 3, 3)] = "执政官",
        [(5, 3, 2)] = "虚空编织者",

        [(6, 1, 1)] = "死亡使者",
        [(6, 1, 2)] = "萨莱因",
        [(6, 2, 1)] = "死亡使者",
        [(6, 2, 3)] = "天启骑士",
        [(6, 3, 3)] = "天启骑士",
        [(6, 3, 2)] = "萨莱因",

        [(7, 1, 1)] = "先知",
        [(7, 1, 2)] = "风暴使者",
        [(7, 2, 2)] = "风暴使者",
        [(7, 2, 3)] = "图腾祭祀",
        [(7, 3, 1)] = "先知",
        [(7, 3, 3)] = "图腾祭祀",

        [(8, 1, 1)] = "疾咒师",
        [(8, 1, 2)] = "日怒",
        [(8, 2, 3)] = "霜火",
        [(8, 2, 2)] = "日怒",
        [(8, 3, 3)] = "霜火",
        [(8, 3, 2)] = "疾咒师",

        [(9, 1, 1)] = "地狱召唤者",
        [(9, 1, 2)] = "灵魂收割者",
        [(9, 2, 3)] = "恶魔使徒",
        [(9, 2, 2)] = "灵魂收割者",
        [(9, 3, 3)] = "恶魔使徒",
        [(9, 3, 2)] = "地狱召唤者",

        [(10, 1, 1)] = "祥和宗师",
        [(10, 1, 2)] = "影踪派",
        [(10, 2, 3)] = "天神御师",
        [(10, 2, 2)] = "祥和宗师",
        [(10, 3, 3)] = "天神御师",
        [(10, 3, 2)] = "影踪派",

        [(11, 1, 1)] = "艾露恩钦选者",
        [(11, 1, 2)] = "丛林守护者",
        [(11, 2, 3)] = "利爪德鲁伊",
        [(11, 2, 4)] = "荒野追猎者",
        [(11, 3, 3)] = "利爪德鲁伊",
        [(11, 3, 2)] = "艾露恩钦选者",
        [(11, 4, 2)] = "丛林守护者",
        [(11, 4, 4)] = "荒野追猎者",

        [(12, 1, 1)] = "奥达奇收割者",
        [(12, 1, 2)] = "邪痕枭雄",
        [(12, 2, 1)] = "奥达奇收割者",
        [(12, 2, 3)] = "歼灭者",
        [(12, 3, 3)] = "歼灭者",
        [(12, 3, 2)] = "虚痕枭雄",

        [(13, 1, 1)] = "塑焰者",
        [(13, 1, 2)] = "鳞长",
        [(13, 2, 3)] = "时空守卫",
        [(13, 2, 2)] = "塑焰者",
        [(13, 3, 3)] = "时空守卫",
        [(13, 3, 2)] = "鳞长"
    };

    public static (string? ClassName, string? SpecName) GetClassAndSpecName(int? classId, int? specId)
    {
        var className = classId is null or 0 ? null : Classes.GetValueOrDefault(classId.Value, $"职业{classId}");
        var specName = classId is null or 0 || specId is null or 0
            ? null
            : Specs.GetValueOrDefault((classId.Value, specId.Value), $"专精{specId}");
        return (className, specName);
    }

    public static IReadOnlyList<(int Id, string Name)> GetClasses()
    {
        return Classes
            .OrderBy(item => item.Key)
            .Select(item => (item.Key, item.Value))
            .ToList();
    }

    public static IReadOnlyList<(int Id, string Name)> GetSpecs(int classId)
    {
        return Specs
            .Where(item => item.Key.ClassId == classId)
            .OrderBy(item => item.Key.SpecId)
            .Select(item => (item.Key.SpecId, item.Value))
            .ToList();
    }

    public static IReadOnlyList<(int Id, string Name)> GetHeroTalents(int classId, int specId)
    {
        return HeroTalents
            .Where(item => item.Key.ClassId == classId && item.Key.SpecId == specId)
            .OrderBy(item => item.Key.HeroTalentId)
            .Select(item => (item.Key.HeroTalentId, item.Value))
            .ToList();
    }
}

