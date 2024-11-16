namespace Austra.Library.Stats;

/// <summary>Contains statistical and other useful math functions.</summary>
public static partial class Functions
{
    private static readonly string[] haikus =
    [
        "古池や　蛙飛び込む　水の音 (Furuike ya/Kawazu tobikomu/Mizu no oto - Basho)",
        "春の海　ひねもすのたり　のたりかな (Haruno-umi Hinemosu-Notari Notarikana - Buson)",
        "痩蛙 負けるな一茶 是にあり (Yase gaeru/Makeruna Issa kore ni ari - Issa)",
        "菜の花や　月は東に　日は西に (Na-no-hana ya/ Tsuki ha higashi ni/ Hi wa nishi ni - Buson)",
        "閑けさや　岩にしみいる　蝉の声 (Shizukesa ya/ Iwa ni shimiiru/ Semi no koe - Basho)",
        "柿くへば　鐘が鳴るなり　法隆寺 (Kaki kueba/ Kane ga naru nari/ Horyuji - Shiki)",
        "目には青葉 山ほとゝぎす はつ松魚 (Me ni wa aoba/Yama hototogisu/Hatsu gatsuo - Sodo)",
        "降る雪や 明治は遠く なりにけり (Furu yuki ya/Meiji wa toku /Nari ni keri - Kusatao)",
        "梅一輪 一輪ほどの 暖かさ (Ume ichi rin/Ichi rin hodo no /Atatakasa - Ransetsu)",
        "朝顔に 釣瓶とられて もらひ水 (Asagao ni/Tsurube torare te/Morai mizu - Chiyome)",
        "書てみたりけしたり果はけしの花 (Kaite mitari/keshitari hate wa/Keshi no hana - Hokushi)",
        "プログラマー コードを書くのが 楽しい仕事 (コパイロット)",
        "So long, and thanks for all the fish. (Douglas Adams)",
    ];

    /// <summary>Retrieves a random haiku.</summary>
    /// <returns>A random haiku.</returns>
    public static string Austra() => haikus[System.Random.Shared.Next(haikus.Length)];
}
