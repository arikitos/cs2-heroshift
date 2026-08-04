using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using static src.HeroShift;
using System.Text.RegularExpressions;
using src.utils;

namespace src.menu
{
    /*
     * Menu - the "!skills" browser: a CenterHtmlMenu listing every loaded hero so
     * players can read what each one does.
     *
     * This is CounterStrikeSharp's own CenterHtmlMenu (the HTML panel drawn in the
     * middle of the screen), not the WASD menu that individual heroes use for
     * target selection - those go through SkillUtils.CreateMenu instead.
     *
     * The list is built from SkillData.Skills, so it contains only definitions
     * whose effective metadata was active at load time. Names and descriptions
     * come from the selected catalog with embedded English fallback, so a new hero
     * appears automatically once it is active and has its translation keys.
     */
    public static class Menu
    {
        // Opens the hero list for one player. Selecting an entry prints that hero's
        // description to chat and closes the menu.
        public static void DisplaySkillsList(CCSPlayerController player)
        {
            CenterHtmlMenu menu = new($"[ ★ {player.GetTranslation("skills_menu")} ★ ]", Instance);
            
            foreach (var skillInfo in SkillData.Skills)
            {
                string skillName = $"{player.GetSkillName(skillInfo.Skill)}";
                menu.AddMenuOption($" ★ {skillName}", (player, option) =>
                {
                    // The display name may carry CenterHtmlMenu "[color=...]" markup and
                    // the decorative star prefix. Both are stripped here so the name
                    // printed to chat is plain text; the regex removes opening and
                    // closing color tags alike.
                    string selectedSkillName = option.Text;
                    string pattern = "\\[/?color\\b[^\\]]*\\]";
                    string cleanSkillName = Regex.Replace(selectedSkillName, pattern, "");
                    string skillName = cleanSkillName.Replace($" ★ ", "");
                    string skillDesc = player.GetSkillDescription(skillInfo.Skill);
                    SkillUtils.PrintToChat(player, $"{ChatColors.DarkRed}{skillName}{ChatColors.Lime}: {skillDesc}", border: "");
                    MenuManager.CloseActiveMenu(player);
                });
            }

            MenuManager.OpenCenterHtmlMenu(Instance, player, menu);
        }
    }
}
