using ECommons.ExcelServices.Sheets;

namespace Lifestream;

internal static class GameText
{
    internal static string Dialogue(string sheet, uint row) =>
        Svc.Data.GetExcelSheet<QuestDialogueText>(name: sheet).GetRow(row).Value.GetText().Trim();
}
