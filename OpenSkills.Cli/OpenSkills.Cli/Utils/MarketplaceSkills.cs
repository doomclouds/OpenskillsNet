namespace OpenSkills.Cli.Utils;

/// <summary>
/// Known skills from Anthropic's marketplace
/// Used to warn about potential conflicts with Claude Code plugins
/// </summary>
public static class MarketplaceSkills
{
    /// <summary>
    /// List of Anthropic marketplace skill names
    /// </summary>
    public static readonly string[] AnthropicMarketplaceSkills =
    [
        // document-skills plugin
        "xlsx",
        "docx",
        "pptx",
        "pdf",

        // example-skills plugin
        "algorithmic-art",
        "artifacts-builder",
        "brand-guidelines",
        "canvas-design",
        "internal-comms",
        "mcp-builder",
        "skill-creator",
        "slack-gif-creator",
        "template-skill",
        "theme-factory",
        "webapp-testing",
    ];
}
