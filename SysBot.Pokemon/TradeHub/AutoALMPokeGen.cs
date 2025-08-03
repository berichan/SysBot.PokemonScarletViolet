using System;
using System.Collections.Generic;
using System.Text;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon
{
    public static class AutoALMPokeGen<T> where T : PKM, new()
    {
        public static T? Generate(int dex, int item, int gamever, int lang, int sid, int tid, int otgender, string OT, string shinyType = "Square", int level = 100, string form = "")
        {
            PKM? toRet;

            var pokePT = PersonalTable.BDSP[dex];
            string nature = pokePT.ATK > pokePT.SPA ? "Adamant" : "Modest";

            try
            {
                StringBuilder sb = new StringBuilder();
                var speciesName = GameInfo.Strings.Species[dex];
                if ((Species)dex == Species.Giratina && item == 112) // Origin form giratina
                    sb.AppendLine($"{speciesName}-Origin @ {GameInfo.Strings.Item[item]}");
                else if ((Species)dex == Species.NidoranM)
                    sb.AppendLine($"Nidoran-M @ {GameInfo.Strings.Item[item]}");
                else if ((Species)dex == Species.NidoranF)
                    sb.AppendLine($"Nidoran-F @ {GameInfo.Strings.Item[item]}");
                else
                    sb.AppendLine($"{speciesName}{(form.Length > 0 ? $"-{form}" : string.Empty)} @ {GameInfo.Strings.Item[item]}");
                sb.AppendLine($"TID: {tid:000000}");
                sb.AppendLine($"SID: {sid:0000}");
                sb.AppendLine($"Shiny: {shinyType}");
                sb.AppendLine($"Level: {level}");
                sb.AppendLine($"{nature} Nature");
                sb.AppendLine(".Moves=$suggestAll");
                sb.AppendLine(".RelearnMoves=$suggestAll");
                sb.AppendLine(".Ribbons=$suggestAll");
                sb.AppendLine(".AffixedRibbon=-1");
                sb.AppendLine(".OT_Gender=" + otgender.ToString());

                if ((Species)dex != Species.Ditto)
                {
                    sb.AppendLine($"OT: {OT}");
                    //sb.AppendLine(".Language=" + lang.ToString());
                    sb.AppendLine("Language: " + ((LanguageID)lang).ToString());
                }
                else
                {

                    sb.AppendLine($"OT: SusSpore");
                    sb.AppendLine("Language: " + ((LanguageID)lang == LanguageID.English ? "English" : "English"));
                }

                var content = sb.ToString();
                var set = new ShowdownSet(content);
                var template = AutoLegalityWrapper.GetTemplate(set);

                var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
                var pkm = sav.GetLegal(template, out var result);
                var la = new LegalityAnalysis(pkm);
                var spec = GameInfo.Strings.Species[template.Species];
                pkm = EntityConverter.ConvertToType(pkm, typeof(T), out _) ?? pkm;
                if (pkm is not T pk || !la.Valid)
                    return null;

                pk.ResetPartyStats();
                toRet = pk;
            }
            catch (Exception ex)
            {
                LogUtil.LogError(ex.Message, "Genner");
                return null;
            }

            if (toRet == null)
                return null;

            if (toRet.Version == (GameVersion)gamever)
                return (T)toRet;
            else
            {
                var toRetEdit = toRet.Clone();
                toRetEdit.Version = (GameVersion)gamever;
                var la = new LegalityAnalysis(toRetEdit);
                if (la.Valid)
                    return (T)toRetEdit;
                else
                    return (T)toRet;
            }
        }

    }
}
