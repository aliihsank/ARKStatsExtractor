﻿using ARKBreedingStats.Library;
using ARKBreedingStats.miscClasses;
using ARKBreedingStats.species;
using ARKBreedingStats.values;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ARKBreedingStats
{
    public partial class Form1
    {
        private void UpdateExtractorDetails()
        {
            panelExtrTE.Visible = rbTamedExtractor.Checked;
            panelExtrImpr.Visible = rbBredExtractor.Checked;
            groupBoxDetailsExtractor.Visible = !rbWildExtractor.Checked;
            if (rbTamedExtractor.Checked)
            {
                groupBoxDetailsExtractor.Text = "Taming-Effectiveness";
            }
            else if (rbBredExtractor.Checked)
            {
                groupBoxDetailsExtractor.Text = "Imprinting-Quality";
            }
        }

        /// <summary>
        /// This displays the sum of the chosen levels. This is the last step before a creature-extraction is considered as valid or not valid.
        /// </summary>
        private void ShowSumOfChosenLevels()
        {
            // The speedlevel is not chosen, but calculated from the other chosen levels, and must not be included in the sum, except all the other levels are determined uniquely!

            // this method will show only the offset of the value, it's less confusing to the user and gives all the infos needed
            int sumW = 0, sumD = 0;
            bool valid = true, inbound = true, allUnique = true;
            for (int s = 0; s < Values.STATS_COUNT; s++)
            {
                if (s == (int)StatNames.Torpidity)
                    continue;
                if (extractor.results[s].Count > extractor.chosenResults[s])
                {
                    sumW += statIOs[s].LevelWild > 0 ? statIOs[s].LevelWild : 0;
                    sumD += statIOs[s].LevelDom;
                    if (extractor.results[s].Count != 1)
                    {
                        allUnique = false;
                    }
                }
                else
                {
                    valid = false;
                    break;
                }
                statIOs[s].TopLevel = StatIOStatus.Neutral;
            }
            if (valid)
            {
                sumW -= allUnique || statIOs[(int)StatNames.SpeedMultiplier].LevelWild < 0 ? 0 : statIOs[(int)StatNames.SpeedMultiplier].LevelWild;
                string offSetWild = "✓";
                lbSumDom.Text = sumD.ToString();
                if (sumW <= extractor.levelWildSum)
                {
                    lbSumWild.ForeColor = SystemColors.ControlText;
                }
                else
                {
                    lbSumWild.ForeColor = Color.Red;
                    offSetWild = "+" + (sumW - extractor.levelWildSum);
                    inbound = false;
                }
                if (sumD == extractor.levelDomSum)
                {
                    lbSumDom.ForeColor = SystemColors.ControlText;
                }
                else
                {
                    lbSumDom.ForeColor = Color.Red;
                    inbound = false;
                    // if there are no other combination options, the total level may be wrong
                    if (extractor.uniqueResults)
                        numericUpDownLevel.BackColor = Color.LightSalmon;
                }
                lbSumWild.Text = offSetWild;
            }
            else
            {
                lbSumWild.Text = "n/a";
                lbSumDom.Text = "n/a";
            }
            panelSums.BackColor = inbound ? SystemColors.Control : Color.FromArgb(255, 200, 200);

            bool torporLevelValid = numericUpDownLevel.Value > statIOs[(int)StatNames.Torpidity].LevelWild;
            if (!torporLevelValid)
            {
                numericUpDownLevel.BackColor = Color.LightSalmon;
                statIOs[(int)StatNames.Torpidity].Status = StatIOStatus.Error;
            }

            bool allValid = valid && inbound && torporLevelValid && extractor.validResults;
            if (allValid)
            {
                radarChartExtractor.setLevels(statIOs.Select(s => s.LevelWild).ToArray());
                toolStripButtonSaveCreatureValuesTemp.Visible = false;
                cbExactlyImprinting.BackColor = Color.Transparent;
                if (topLevels.TryGetValue(speciesSelector1.SelectedSpecies, out int[] topSpeciesLevels))
                {
                    for (int s = 0; s < Values.STATS_COUNT; s++)
                    {
                        if (s == (int)StatNames.Torpidity)
                            continue;
                        if (statIOs[s].LevelWild > 0)
                        {
                            if (statIOs[s].LevelWild == topSpeciesLevels[s])
                                statIOs[s].TopLevel = StatIOStatus.TopLevel;
                            else if (topSpeciesLevels[s] != -1 && statIOs[s].LevelWild > topSpeciesLevels[s])
                                statIOs[s].TopLevel = StatIOStatus.NewTopLevel;
                        }
                    }
                }
            }
            creatureInfoInputExtractor.ButtonEnabled = allValid;
            groupBoxRadarChartExtractor.Visible = allValid;
        }

        /// <summary>
        /// Clears all the parameters of the extractor.
        /// </summary>
        /// <param name="clearExtraCreatureData">Also delete infos like the guid, parents and mutations</param>
        private void ClearAll(bool clearExtraCreatureData = true)
        {
            extractor.Clear();
            listViewPossibilities.Items.Clear();
            for (int s = 0; s < Values.STATS_COUNT; s++)
            {
                statIOs[s].Clear();
            }
            ExtractionFailed(); // set background of controls to neutral
            labelFootnote.Text = string.Empty;
            labelFootnote.BackColor = Color.Transparent;
            labelTE.Text = string.Empty;
            activeStatIndex = -1;
            lbSumDom.Text = string.Empty;
            lbSumWild.Text = string.Empty;
            lbSumDomSB.Text = string.Empty;
            updateTorporInTester = true;
            creatureInfoInputExtractor.ButtonEnabled = false;
            groupBoxPossibilities.Visible = false;
            groupBoxRadarChartExtractor.Visible = false;
            lbInfoYellowStats.Visible = false;
            button2TamingCalc.Visible = cbQuickWildCheck.Checked;
            groupBoxTamingInfo.Visible = cbQuickWildCheck.Checked;
            UpdateQuickTamingInfo();
            labelTamingInfo.Text = string.Empty;
            SetMessageLabelText();
            if (clearExtraCreatureData)
            {
                creatureInfoInputExtractor.MutationCounterMother = 0;
                creatureInfoInputExtractor.MutationCounterFather = 0;
                creatureInfoInputExtractor.DomesticatedAt = DateTime.Now;
                creatureInfoInputExtractor.parentListValid = false;
                creatureInfoInputExtractor.CreatureGuid = Guid.Empty;
                exportedCreatureControl = null;
                creatureInfoInputExtractor.SetArkId(0, false);
            }

            creatureInfoInputExtractor.UpdateExistingCreature = false;
        }

        private void buttonExtract_Click(object sender, EventArgs e)
        {
            ExtractLevels();
        }

        /// <summary>
        /// Extract the levels by using the inputs of the statIOs and various other controls of the extractor.
        /// </summary>
        /// <param name="autoExtraction"></param>
        /// <param name="statInputsHighPrecision">Set to true if the data is from an export file which has a higher precision for stat-values so the tolerance of calculations can be smaller.</param>
        /// <returns></returns>
        private bool ExtractLevels(bool autoExtraction = false, bool statInputsHighPrecision = false, bool showLevelsInOverlay = false, Creature existingCreature = null)
        {
            SuspendLayout();
            int activeStatKeeper = activeStatIndex;
            ClearAll(clearExtractionCreatureData);

            if (cbExactlyImprinting.Checked)
                extractor.possibleIssues |= IssueNotes.Issue.ImprintingLocked;

            extractor.ExtractLevels(speciesSelector1.SelectedSpecies, (int)numericUpDownLevel.Value, statIOs,
                    (double)numericUpDownLowerTEffBound.Value / 100, (double)numericUpDownUpperTEffBound.Value / 100,
                    rbTamedExtractor.Checked, rbBredExtractor.Checked,
                    (double)numericUpDownImprintingBonusExtractor.Value / 100, !cbExactlyImprinting.Checked,
                    creatureCollection.allowMoreThanHundredImprinting, creatureCollection.serverMultipliers.BabyImprintingStatScaleMultiplier,
                    Values.V.currentServerMultipliers.BabyCuddleIntervalMultiplier,
                    creatureCollection.considerWildLevelSteps, creatureCollection.wildLevelStep, statInputsHighPrecision, out bool imprintingBonusChanged);

            numericUpDownImprintingBonusExtractor.ValueSave = (decimal)extractor.ImprintingBonus * 100;
            numericUpDownImprintingBonusExtractor_ValueChanged(null, null);

            if (imprintingBonusChanged && !autoExtraction)
            {
                extractor.possibleIssues |= IssueNotes.Issue.ImprintingNotPossible;
            }

            bool everyStatHasAtLeastOneResult = extractor.EveryStatHasAtLeastOneResult;

            // remove all results that require a total wild-level higher than the max
            // Tek-variants have 20% higher levels
            extractor.RemoveImpossibleTEsAccordingToMaxWildLevel((int)Math.Ceiling(creatureCollection.maxWildLevel * (speciesSelector1.SelectedSpecies.name.StartsWith("Tek ") ? 1.2 : 1)));

            if (everyStatHasAtLeastOneResult && !extractor.EveryStatHasAtLeastOneResult)
            {
                MessageBox.Show(string.Format(Loc.s("issueMaxWildLevelTooLow"), creatureCollection.maxWildLevel), "ASB: Maybe the wild max level is set too low", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            if (!extractor.SetStatLevelBoundsAndFilter(out int statIssue))
            {
                if (statIssue == -1)
                {
                    ExtractionFailed(IssueNotes.Issue.WildTamedBred
                            | IssueNotes.Issue.CreatureLevel
                            | (rbTamedExtractor.Checked ? IssueNotes.Issue.TamingEffectivenessRange : IssueNotes.Issue.None));
                    ResumeLayout();
                    return false;
                }
                extractor.possibleIssues |= IssueNotes.Issue.Typo | IssueNotes.Issue.CreatureLevel;
                statIOs[statIssue].Status = StatIOStatus.Error;
                statIOs[(int)StatNames.Torpidity].Status = StatIOStatus.Error;
            }

            // get mean-level (most probable for the wild levels)
            // TODO handle species without wild levels in speed better (some flyers)
            double meanWildLevel = Math.Round((double)extractor.levelWildSum / 7, 1);
            bool nonUniqueStats = false;

            for (int s = 0; s < Values.STATS_COUNT; s++)
            {
                if (!activeStats[s])
                {
                    statIOs[s].Status = StatIOStatus.Neutral;
                }
                else if (extractor.results[s].Any())
                {
                    if (existingCreature != null)
                    {
                        // set the wild levels to the existing ones
                        int r = 0;
                        for (int b = 1; b < extractor.results[s].Count; b++)
                        {
                            if (extractor.results[s][b].levelWild == existingCreature.levelsWild[s]
                                && extractor.results[s][b].levelDom >= existingCreature.levelsDom[s]
                                && extractor.results[s][b].TE.Includes(existingCreature.tamingEff))
                            {
                                r = b;
                                break;
                            }
                        }
                        SetLevelCombination(s, r == -1 ? 0 : r);
                    }
                    else
                    {
                        // choose the most probable wild-level, aka the level nearest to the mean of the wild levels.
                        int r = 0;
                        for (int b = 1; b < extractor.results[s].Count; b++)
                        {
                            if (Math.Abs(meanWildLevel - extractor.results[s][b].levelWild) < Math.Abs(meanWildLevel - extractor.results[s][r].levelWild))
                                r = b;
                        }

                        SetLevelCombination(s, r);
                    }
                    if (extractor.results[s].Count > 1)
                    {
                        statIOs[s].Status = StatIOStatus.Nonunique;
                        nonUniqueStats = true;
                    }
                    else
                    {
                        statIOs[s].Status = StatIOStatus.Unique;
                    }
                }
                else
                {
                    // no results for this stat
                    statIOs[s].Status = StatIOStatus.Error;
                    extractor.validResults = false;
                    if (rbTamedExtractor.Checked && extractor.statsWithTE.Contains(s))
                    {
                        extractor.possibleIssues |= IssueNotes.Issue.TamingEffectivenessRange;
                    }
                    // if the stat is changed by singleplayer-settings, list that as a possible issue
                    if (s == (int)StatNames.Health
                        || s == (int)StatNames.MeleeDamageMultiplier)
                    {
                        extractor.possibleIssues |= IssueNotes.Issue.Singleplayer;
                    }
                }
            }
            if (!extractor.validResults)
            {
                ExtractionFailed(IssueNotes.Issue.Typo | IssueNotes.Issue.WildTamedBred | IssueNotes.Issue.LockedDom |
                        IssueNotes.Issue.OutdatedIngameValues | IssueNotes.Issue.ImprintingNotUpdated |
                        (statIOs[(int)StatNames.Torpidity].LevelWild >= (int)numericUpDownLevel.Value ? IssueNotes.Issue.CreatureLevel : IssueNotes.Issue.None));
                ResumeLayout();
                return false;
            }
            extractor.uniqueResults = !nonUniqueStats;
            if (!extractor.uniqueResults)
            {
                groupBoxPossibilities.Visible = true;
                lbInfoYellowStats.Visible = true;
            }

            // if damage has a possibility for the dom-levels to make it a valid sum, take this
            int domLevelsChosenSum = 0;
            for (int s = 0; s < Values.STATS_COUNT; s++)
            {
                if (s != (int)StatNames.Torpidity)
                    domLevelsChosenSum += extractor.results[s][extractor.chosenResults[s]].levelDom;
            }
            if (domLevelsChosenSum != extractor.levelDomSum)
            {
                // sum of domlevels is not correct. Try to find another combination
                domLevelsChosenSum -= extractor.results[(int)StatNames.MeleeDamageMultiplier][extractor.chosenResults[(int)StatNames.MeleeDamageMultiplier]].levelDom;
                bool changeChosenResult = false;
                int cR = 0;
                for (int r = 0; r < extractor.results[(int)StatNames.MeleeDamageMultiplier].Count; r++)
                {
                    if (domLevelsChosenSum + extractor.results[(int)StatNames.MeleeDamageMultiplier][r].levelDom == extractor.levelDomSum)
                    {
                        cR = r;
                        changeChosenResult = true;
                        break;
                    }
                }
                if (changeChosenResult)
                    SetLevelCombination((int)StatNames.MeleeDamageMultiplier, cR);
            }

            if (extractor.postTamed)
                SetUniqueTE();
            else
            {
                labelTE.Text = Loc.s("notYetTamed");
                labelTE.BackColor = Color.Transparent;
            }

            SetWildSpeedLevelAccordingToOthers();

            lbSumDomSB.Text = extractor.levelDomSum.ToString();
            ShowSumOfChosenLevels();
            if (showLevelsInOverlay)
                ShowLevelsInOverlay();

            SetActiveStat(activeStatKeeper);

            if (!extractor.postTamed)
            {
                labelFootnote.Text = Loc.s("lbNotYetTamed");
                button2TamingCalc.Visible = true;

                // display taming info
                SetQuickTamingInfo(cbQuickWildCheck.Checked
                    ? statIOs[(int)StatNames.Torpidity].LevelWild + 1
                    : (int)numericUpDownLevel.Value);
            }

            ResumeLayout();
            return true;
        }

        private void UpdateQuickTamingInfo()
        {
            bool showQuickTamingInfo = cbQuickWildCheck.Checked;
            if (showQuickTamingInfo)
            {
                for (int s = 0; s < Values.STATS_COUNT; s++)
                {
                    int lvlWild = (int)Math.Round((statIOs[s].Input - speciesSelector1.SelectedSpecies.stats[s].BaseValue) / (speciesSelector1.SelectedSpecies.stats[s].BaseValue * speciesSelector1.SelectedSpecies.stats[s].IncPerWildLevel));
                    statIOs[s].LevelWild = lvlWild < 0 ? 0 : lvlWild;
                    statIOs[s].LevelDom = 0;
                }
                SetQuickTamingInfo(statIOs[(int)StatNames.Torpidity].LevelWild + 1);
            }
            panelWildTamedBred.Enabled = !showQuickTamingInfo;
            groupBoxDetailsExtractor.Enabled = !showQuickTamingInfo;
            numericUpDownLevel.Enabled = !showQuickTamingInfo;
            button2TamingCalc.Visible = showQuickTamingInfo;
            groupBoxTamingInfo.Visible = showQuickTamingInfo;
        }

        private void SetQuickTamingInfo(int level)
        {
            tamingControl1.SetSpecies(speciesSelector1.SelectedSpecies);
            tamingControl1.SetLevel(level);
            labelTamingInfo.Text = tamingControl1.quickTamingInfos;
            groupBoxTamingInfo.Visible = true;
        }

        /// <summary>
        /// Call this method if an extraction failed. Possible causes of the failure are displayed for the user.
        /// </summary>
        /// <param name="issues"></param>
        private void ExtractionFailed(IssueNotes.Issue issues = IssueNotes.Issue.None)
        {
            issues |= extractor.possibleIssues; // add all issues that arised during extraction
            if (issues == IssueNotes.Issue.None)
            {
                // set background of inputs to neutral
                numericUpDownLevel.BackColor = SystemColors.Window;
                numericUpDownLowerTEffBound.BackColor = SystemColors.Window;
                numericUpDownUpperTEffBound.BackColor = SystemColors.Window;
                numericUpDownImprintingBonusExtractor.BackColor = SystemColors.Window;
                cbExactlyImprinting.BackColor = Color.Transparent;
                panelSums.BackColor = Color.Transparent;
                panelWildTamedBred.BackColor = Color.Transparent;
                labelTE.BackColor = Color.Transparent;
                llOnlineHelpExtractionIssues.Visible = false;
                labelErrorHelp.Visible = false;
                lbImprintingFailInfo.Visible = false; // TODO move imprinting-fail to upper note-info
                extractor.possibleIssues = IssueNotes.Issue.None;
            }
            else
            {
                // highlight controls which most likely need to be checked to solve the issue
                if (issues.HasFlag(IssueNotes.Issue.WildTamedBred))
                    panelWildTamedBred.BackColor = Color.LightSalmon;
                if (issues.HasFlag(IssueNotes.Issue.TamingEffectivenessRange))
                {
                    if (numericUpDownLowerTEffBound.Value > 0)
                        numericUpDownLowerTEffBound.BackColor = Color.LightSalmon;
                    if (numericUpDownUpperTEffBound.Value < 100)
                        numericUpDownUpperTEffBound.BackColor = Color.LightSalmon;
                    if (numericUpDownLowerTEffBound.Value == 0 && numericUpDownUpperTEffBound.Value == 100)
                        issues -= IssueNotes.Issue.TamingEffectivenessRange;
                }
                if (issues.HasFlag(IssueNotes.Issue.CreatureLevel))
                {
                    numericUpDownLevel.BackColor = Color.LightSalmon;
                    numericUpDownImprintingBonusExtractor.BackColor = Color.LightSalmon;
                    statIOs[(int)StatNames.Torpidity].Status = StatIOStatus.Error;
                }
                if (issues.HasFlag(IssueNotes.Issue.ImprintingLocked))
                    cbExactlyImprinting.BackColor = Color.LightSalmon;
                if (issues.HasFlag(IssueNotes.Issue.ImprintingNotPossible))
                    numericUpDownImprintingBonusExtractor.BackColor = Color.LightSalmon;

                // don't show some issue notes if the input is not wrong
                if (issues.HasFlag(IssueNotes.Issue.LockedDom))
                {
                    bool oneStatIsDomLocked = false;
                    for (int s = 0; s < Values.STATS_COUNT; s++)
                    {
                        if (statIOs[s].DomLevelLockedZero)
                        {
                            oneStatIsDomLocked = true;
                            break;
                        }
                    }
                    if (!oneStatIsDomLocked)
                    {
                        // no stat is domLocked, remove this note (which is ensured to be there)
                        issues -= IssueNotes.Issue.LockedDom;
                    }
                }

                if (!issues.HasFlag(IssueNotes.Issue.StatMultipliers))
                    issues |= IssueNotes.Issue.StatMultipliers; // add this always?

                if (rbTamedExtractor.Checked && creatureCollection.considerWildLevelSteps)
                    issues |= IssueNotes.Issue.WildLevelSteps;

                labelErrorHelp.Text = "The extraction failed. See the following list of possible causes:\n\n" +
                        IssueNotes.getHelpTexts(issues);
                labelErrorHelp.Visible = true;
                llOnlineHelpExtractionIssues.Visible = true;
                groupBoxPossibilities.Visible = false;
                groupBoxRadarChartExtractor.Visible = false;
                lbInfoYellowStats.Visible = false;
                if (rbBredExtractor.Checked && numericUpDownImprintingBonusExtractor.Value > 0)
                {
                    lbImprintingFailInfo.Text = Loc.s("lbImprintingFailInfo");
                    lbImprintingFailInfo.Visible = true;
                }
                else if (rbTamedExtractor.Checked
                    && "Desert Titan,Desert Titan Flock,Ice Titan,Gacha,Aberrant Electrophorus,Electrophorus,Aberrant Pulmonoscorpius,Pulmonoscorpius,Aberrant Titanoboa,Titanoboa,Pegomastax,Procoptodon,Troodon"
                    .Split(',').ToList().Contains(speciesSelector1.SelectedSpecies.name))
                {
                    // creatures that display wrong stat-values after taming
                    lbImprintingFailInfo.Text = $"The {speciesSelector1.SelectedSpecies.name} is known for displaying wrong stat-values after taming. " +
                            "This can prevent a successful extraction. Currently there's no known fix for that issue.";
                    lbImprintingFailInfo.Visible = true;
                }
                toolStripButtonSaveCreatureValuesTemp.Visible = true;

                // check for updates
                if (DateTime.Now.AddHours(-5) > Properties.Settings.Default.lastUpdateCheck)
                    CheckForUpdates(true);
            }
        }

        /// <summary>
        /// If a stat has multiple possibilities for its level distribution, the taming effectiveness may be affected by that.
        /// Set the TE of the selected levels.
        /// </summary>
        private void SetUniqueTE()
        {
            double te = Math.Round(extractor.UniqueTE(), 5);
            if (te >= 0)
            {
                labelTE.Text = $"Extracted: {Math.Round(100 * te, 2)} %";
                if (rbTamedExtractor.Checked && extractor.postTamed)
                    labelTE.Text += $" (wildlevel: {Creature.CalculatePreTameWildLevel(statIOs[(int)StatNames.Torpidity].LevelWild + 1, te)})";
                labelTE.BackColor = Color.Transparent;
            }
            else
            {
                if (te == -1)
                {
                    labelTE.Text = "TE differs in chosen possibilities";
                    labelTE.BackColor = Color.LightSalmon;
                }
                else
                {
                    labelTE.Text = "TE unknown";
                }
            }
        }

        /// <summary>
        /// When clicking on a stat show the possibilites in the listbox.
        /// </summary>
        /// <param name="statIndex"></param>
        private void SetActiveStat(int statIndex)
        {
            if (statIndex != activeStatIndex)
            {
                activeStatIndex = -1;
                listViewPossibilities.BeginUpdate();
                listViewPossibilities.Items.Clear();
                for (int s = 0; s < Values.STATS_COUNT; s++)
                {
                    if (s == statIndex && statIOs[s].Status == StatIOStatus.Nonunique)
                    {
                        statIOs[s].Selected = true;
                        SetPossibilitiesListview(s);
                        activeStatIndex = statIndex;
                    }
                    else
                    {
                        statIOs[s].Selected = false;
                    }
                }
                listViewPossibilities.EndUpdate();
            }
        }

        /// <summary>
        /// Fill listbox with possible results of stat
        /// </summary>
        /// <param name="s"></param>
        private void SetPossibilitiesListview(int s)
        {
            if (s < extractor.results.Length)
            {
                bool resultsValid = extractor.FilterResultsByFixed(s) == -1;
                for (int r = 0; r < extractor.results[s].Count; r++)
                {
                    List<string> subItems = new List<string>();
                    double te = Math.Round(extractor.results[s][r].TE.Mean, 5);
                    subItems.Add(extractor.results[s][r].levelWild.ToString());
                    subItems.Add(extractor.results[s][r].levelDom.ToString());
                    subItems.Add(te >= 0 ? (te * 100).ToString() : string.Empty);

                    subItems.Add(te > 0 ? Creature.CalculatePreTameWildLevel(extractor.levelWildSum + 1, te).ToString() : string.Empty);

                    ListViewItem lvi = new ListViewItem(subItems.ToArray());
                    if (!resultsValid || extractor.results[s][r].currentlyNotValid)
                        lvi.BackColor = Color.LightSalmon;
                    if (extractor.fixedResults[s] && extractor.chosenResults[s] == r)
                    {
                        lvi.BackColor = Color.LightSkyBlue;
                    }

                    lvi.Tag = r;

                    listViewPossibilities.Items.Add(lvi);
                }
            }
        }

        private void listViewPossibilities_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listViewPossibilities.SelectedIndices.Count > 0)
            {
                int index = (int)listViewPossibilities.SelectedItems[0].Tag;
                if (index >= 0 && activeStatIndex >= 0)
                {
                    SetLevelCombination(activeStatIndex, index, true);
                    extractor.fixedResults[activeStatIndex] = true;
                }
            }
            else if (activeStatIndex >= 0)
                extractor.fixedResults[activeStatIndex] = false;
        }

        /// <summary>
        /// Set an option for a stat that has multiple possibilities.
        /// </summary>
        /// <param name="s">Stat index</param>
        /// <param name="i">Option index</param>
        /// <param name="validateCombination"></param>
        private void SetLevelCombination(int s, int i, bool validateCombination = false)
        {
            statIOs[s].LevelWild = extractor.results[s][i].levelWild;
            statIOs[s].LevelDom = extractor.results[s][i].levelDom;
            statIOs[s].BreedingValue = StatValueCalculation.CalculateValue(speciesSelector1.SelectedSpecies, s, extractor.results[s][i].levelWild, 0, true, 1, 0);
            extractor.chosenResults[s] = i;
            if (validateCombination)
            {
                SetUniqueTE();
                SetWildSpeedLevelAccordingToOthers();
                ShowSumOfChosenLevels();
            }
        }

        /// <summary>
        /// The wild speed level is calculated indirectly by using all unused stat-levels.
        /// </summary>
        private void SetWildSpeedLevelAccordingToOthers()
        {
            // wild speed level is wildTotalLevels - determinedWildLevels. sometimes the oxygenlevel cannot be determined as well
            bool unique = true;
            int notDeterminedLevels = statIOs[(int)StatNames.Torpidity].LevelWild;
            for (int s = 0; s < Values.STATS_COUNT; s++)
            {
                if (s == (int)StatNames.SpeedMultiplier || s == (int)StatNames.Torpidity)
                    continue;
                if (statIOs[s].LevelWild >= 0)
                {
                    notDeterminedLevels -= statIOs[s].LevelWild;
                }
                else
                {
                    unique = false;
                    break;
                }
            }
            if (unique)
            {
                // if all other stats are unique, set speedlevel
                statIOs[(int)StatNames.SpeedMultiplier].LevelWild = Math.Max(0, notDeterminedLevels);
                statIOs[(int)StatNames.SpeedMultiplier].BreedingValue = StatValueCalculation.CalculateValue(speciesSelector1.SelectedSpecies, (int)StatNames.SpeedMultiplier, statIOs[(int)StatNames.SpeedMultiplier].LevelWild, 0, true, 1, 0);
            }
            else
            {
                // if not all other levels are unique, set speed and not known levels to unknown
                for (int s = 0; s < Values.STATS_COUNT; s++)
                {
                    if (s == (int)StatNames.SpeedMultiplier || !activeStats[s])
                    {
                        statIOs[s].LevelWild = -1;
                    }
                }
            }
        }

        private void CopyExtractionToClipboard()
        {
            bool header = true;
            bool table = MessageBox.Show("Results can be copied as own table or as a long table-row. Should it be copied as own table?",
                    "Copy as own table?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
            if (extractor.validResults && speciesSelector1.SelectedSpecies != null)
            {
                List<string> tsv = new List<string>();
                string rowLevel = speciesSelector1.SelectedSpecies.name + "\t\t";
                string rowValues = string.Empty;
                // if taming effectiveness is unique, display it, too
                string effString = string.Empty;
                double eff = extractor.UniqueTE();
                if (eff >= 0)
                {
                    effString = "\tTamingEff:\t" + (100 * eff) + "%";
                }
                // headerrow
                if (table || header)
                {
                    if (table)
                    {
                        tsv.Add(speciesSelector1.SelectedSpecies.name + "\tLevel " + numericUpDownLevel.Value + effString);
                        tsv.Add("Stat\tWildLevel\tDomLevel\tBreedingValue");
                    }
                    else
                    {
                        tsv.Add("Species\tName\tSex\tHP-Level\tSt-Level\tOx-Level\tFo-Level\tWe-Level\tDm-Level\tSp-Level\tTo-Level\tHP-Value\tSt-Value\tOx-Value\tFo-Value\tWe-Value\tDm-Value\tSp-Value\tTo-Value");
                    }
                }
                for (int s = 0; s < Values.STATS_COUNT; s++)
                {
                    if (extractor.chosenResults[s] < extractor.results[s].Count)
                    {
                        string breedingV = string.Empty;
                        if (activeStats[s])
                        {
                            breedingV = statIOs[s].BreedingValue.ToString();
                        }
                        if (table)
                        {
                            tsv.Add(Utils.StatName(s) + "\t" + (statIOs[s].LevelWild >= 0 ? statIOs[s].LevelWild.ToString() : string.Empty) + "\t" + (statIOs[s].LevelWild >= 0 ? statIOs[s].LevelWild.ToString() : string.Empty) + "\t" + breedingV);
                        }
                        else
                        {
                            rowLevel += "\t" + (activeStats[s] ? statIOs[s].LevelWild.ToString() : string.Empty);
                            rowValues += "\t" + breedingV;
                        }
                    }
                    else
                    {
                        return;
                    }
                }
                if (!table)
                {
                    tsv.Add(rowLevel + rowValues);
                }
                Clipboard.SetText(string.Join("\n", tsv));
            }
        }

        /// <summary>
        /// Export the given creature export file in the extractor.
        /// Returns true if the creature already exists in the library.
        /// </summary>
        /// <param name="exportFile"></param>
        private bool ExtractExportedFileInExtractor(string exportFile)
        {
            var cv = importExported.ImportExported.importExportedCreature(exportFile);
            if (cv == null)
            {
                MessageBox.Show("Exported creature-file not recognized.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            // check if last exported file is a species that should be ignored, e.g. a raft
            if (Values.V.IgnoreSpeciesBlueprint(cv.speciesBlueprint))
            {
                MessageBox.Show("Species of last exported creature is ignored" + (cv.speciesBlueprint.Contains("Raft") ? " because it's a raft" : string.Empty) + ":\n" + cv.speciesBlueprint, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // check if species is supported.
            if (cv.Species == null)
            {
                CheckForMissingModFiles(creatureCollection, new List<string> { cv.speciesBlueprint });

                int oldModHash = creatureCollection.modListHash;
                // if mods were added, try to import the creature values again
                if (creatureCollection.ModValueReloadNeeded
                    && LoadModValuesOfCollection(creatureCollection, true, true)
                    && oldModHash != creatureCollection.modListHash)
                    ExtractExportedFileInExtractor(exportFile);

                return false;
            }


            bool creatureExists = ExtractValuesInExtractor(cv, exportFile, true);

            if (Properties.Settings.Default.applyNamePatternOnImportIfEmptyName
                && string.IsNullOrEmpty(creatureInfoInputExtractor.CreatureName))
            {
                CreatureInfoInput_CreatureDataRequested(creatureInfoInputExtractor, false, false, 0);
                if (Properties.Settings.Default.copyNameToClipboardOnImportWhenAutoNameApplied)
                {
                    if (string.IsNullOrEmpty(creatureInfoInputExtractor.CreatureName))
                        Clipboard.SetText("<no name>");
                    else
                        Clipboard.SetText(creatureInfoInputExtractor.CreatureName);
                }
            }

            tabControlMain.SelectedTab = tabPageExtractor;
            return creatureExists;
        }

        /// <summary>
        /// Export the creature export file of the given exportedCreature control.
        /// </summary>
        /// <param name="ecc"></param>
        /// <param name="updateParentVisuals"></param>
        private void ExtractExportedFileInExtractor(importExported.ExportedCreatureControl ecc, bool updateParentVisuals = false)
        {
            if (ecc == null)
                return;

            ExtractValuesInExtractor(ecc.creatureValues, ecc.exportedFile, false);

            // gets deleted in extractLevels()
            exportedCreatureControl = ecc;

            if (exportedCreatureList != null && exportedCreatureList.ownerSuffix != null)
                creatureInfoInputExtractor.CreatureOwner += exportedCreatureList.ownerSuffix;
        }

        private bool ExtractValuesInExtractor(CreatureValues cv, string filePath, bool autoExtraction)
        {
            SetCreatureValuesToExtractor(cv, false);

            // exported stat-files have values for all stats, so activate all stats the species uses
            SetStatsActiveAccordingToUsage(cv.Species);

            bool creatureExists = IsCreatureAlreadyInLibrary(cv.guid, cv.ARKID, out Creature existingCreature);

            ExtractLevels(autoExtraction: autoExtraction, statInputsHighPrecision: true, existingCreature: existingCreature);
            UpdateParentListInput(creatureInfoInputExtractor); // this function is only used for single-creature extractions, e.g. LastExport
            SetCreatureValuesToInfoInput(cv, creatureInfoInputExtractor);
            creatureInfoInputExtractor.UpdateExistingCreature = creatureExists;
            SetMessageLabelText("Creature of the exported file\n" + filePath);
            return creatureExists;
        }

        /// <summary>
        /// Enable stats in extractor according to which stats the species uses.
        /// </summary>
        /// <param name="species"></param>
        private void SetStatsActiveAccordingToUsage(Species species)
        {
            for (int s = 0; s < Values.STATS_COUNT; s++)
            {
                activeStats[s] = species.UsesStat(s);
                statIOs[s].IsActive = activeStats[s];
            }
        }

        /// <summary>
        /// Set the stat-values to the extractor.
        /// </summary>
        /// <param name="cv"></param>
        /// <param name="setInfoInput"></param>
        private void SetCreatureValuesToExtractor(CreatureValues cv, bool setInfoInput = true)
        {
            // at this point, if the creatureValues has parent-ArkIds, make sure these parent-creatures exist
            if (cv.Mother == null)
            {
                if (creatureCollection.CreatureById(cv.motherGuid, cv.motherArkId, cv.Species, cv.sex, out Creature mother))
                {
                    cv.Mother = mother;
                }
                else if (cv.motherArkId != 0)
                {
                    cv.Mother = new Creature(cv.motherArkId);
                    creatureCollection.creatures.Add(cv.Mother);
                }
            }
            if (cv.Father == null)
            {
                if (creatureCollection.CreatureById(cv.fatherGuid, cv.fatherArkId, cv.Species, cv.sex, out Creature father))
                {
                    cv.Father = father;
                }
                else if (cv.fatherArkId != 0)
                {
                    cv.Father = new Creature(cv.fatherArkId);
                    creatureCollection.creatures.Add(cv.Father);
                }
            }

            ClearAll();
            speciesSelector1.SetSpecies(Values.V.SpeciesByBlueprint(cv.speciesBlueprint));
            for (int s = 0; s < Values.STATS_COUNT; s++)
                statIOs[s].Input = cv.statValues[s];

            if (setInfoInput)
                SetCreatureValuesToInfoInput(cv, creatureInfoInputExtractor);

            numericUpDownLevel.ValueSave = cv.level;
            numericUpDownLowerTEffBound.ValueSave = (decimal)cv.tamingEffMin * 100;
            numericUpDownUpperTEffBound.ValueSave = (decimal)cv.tamingEffMax * 100;

            if (cv.isBred)
                rbBredExtractor.Checked = true;
            else if (cv.isTamed)
                rbTamedExtractor.Checked = true;
            else
                rbWildExtractor.Checked = true;
            numericUpDownImprintingBonusExtractor.ValueSave = (decimal)cv.imprintingBonus * 100;
        }

        /// <summary>
        /// Gives feedback to the user if the current creature in the extractor is already in the library.
        /// This uses the ARK-ID and only works if exported creatures are imported
        /// </summary>
        private bool IsCreatureAlreadyInLibrary(Guid creatureGuid, long arkId, out Creature existingCreature)
        {
            existingCreature = null;
            bool creatureAlreadyExistsInLibrary = false;
            if (creatureGuid != Guid.Empty
                                                  && Utils.IsArkIdImported(arkId, creatureGuid))
            {
                existingCreature = creatureCollection.creatures.FirstOrDefault(c => c.guid == creatureGuid
                                                           && !c.flags.HasFlag(CreatureFlags.Placeholder)
                                                                  );
                if (existingCreature != null)
                    creatureAlreadyExistsInLibrary = true;
            }
            return creatureAlreadyExistsInLibrary;
        }

        private void creatureInfoInputExtractor_Add2Library_Clicked(CreatureInfoInput sender)
        {
            AddCreatureToCollection();
        }
    }
}
