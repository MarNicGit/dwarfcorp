using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class EmployeePanel : Gui.Widget
    {
        public WorldManager World;
        private Gui.Widgets.WidgetListView EmployeeList;
        private EditableTextField FilterTextField;

        private string GetFilter()
        {
            if (FilterTextField != null)
                return FilterTextField.Text;
            return "";
        }

        private string GetMinionDescriptorString(CreatureAI Minion)
        {
            var builder = new StringBuilder();
            if (Minion.Stats.Species.HasValue(out var species))
                builder.Append(species.Name);
            builder.Append(Minion.Stats.Gender);
            builder.Append(Minion.Stats.FullName);
            builder.Append(Minion.Stats.GetCurrentLevel());
            builder.Append(Minion.Stats.Title);
            if (Minion.Stats.IsOverQualified) builder.Append("wants promotion");
            if (Minion.Stats.IsOnStrike) builder.Append("strike");
            if (Minion.Stats.IsAsleep) builder.Append("asleeping");
            return builder.ToString().ToUpper();
        }

        private bool PassesFilter(CreatureAI Minion)
        {
            var filter = GetFilter();
            if (String.IsNullOrEmpty(filter))
                return true;
            return GetMinionDescriptorString(Minion).Contains(filter.ToUpper());
        }

        private void RebuildEmployeeList()
        {
            EmployeeList.ClearItems();

            EmployeeList.AddItem(new Widget
            {
                Text = "+ Hire New Employee",
                TextColor = new Vector4(0,0,0,1),
                MinimumSize = new Point(128, 64),
                OnClick = (sender, args) =>
                {
                    // Show hire dialog.
                    var dialog = Root.ConstructWidget(
                        new HireEmployeeDialog(World.PlayerFaction.Economy.Information)
                        {
                            World = World,
                            OnClose = (_s) =>
                            {
                                EmployeeList.Hidden = false;
                                RebuildEmployeeList();
                            }
                        });
                    Root.ShowModalPopup(dialog);
                    World.Tutorial("hire");
                    EmployeeList.Hidden = true;
                }
            });

            foreach (var employee in World.PlayerFaction.Minions.Where(m => PassesFilter(m)))
            {
                var bar = Root.ConstructWidget(new Widget
                {
                    Background = new TileReference("basic", 0),
                    Tag = employee
                });

                if (employee.GetRoot().GetComponent<DwarfSprites.DwarfCharacterSprite>().HasValue(out var employeeSprite))
                    bar.AddChild(new EmployeePortrait
                    {
                        AutoLayout = AutoLayout.DockLeft,
                        MinimumSize = new Point(48, 40),
                        MaximumSize = new Point(48, 40),
                        Sprite = employeeSprite.GetLayers(),
                        AnimationPlayer = employeeSprite.AnimPlayer
                    });

                bar.AddChild(new Widget
                {
                    AutoLayout = AutoLayout.DockFill,
                    TextVerticalAlign = VerticalAlign.Center,
                    MinimumSize = new Point(128, 64),
                    Text = (employee.Stats.IsOverQualified ? employee.Stats.FullName + "*" : employee.Stats.FullName) + " (" + employee.Stats.Title + ")"
                });

                EmployeeList.AddItem(bar);
            }

            EmployeeList.SelectedIndex = 1;
        }

        public override void Construct()
        {
            var topBar = AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 32),
                Padding = new Margin(2,2,2,2)
            });

            topBar.AddChild(new Widget
            {
                MinimumSize = new Point(64, 0),
                AutoLayout = AutoLayout.DockLeft,
                Text = "Filter:",
                TextVerticalAlign = VerticalAlign.Center
            });

            FilterTextField = topBar.AddChild(new EditableTextField
            {
                AutoLayout = AutoLayout.DockFill,
                OnTextChange = (sender) => RebuildEmployeeList()
            }) as EditableTextField;

            EmployeeList = AddChild(new Gui.Widgets.WidgetListView
            {
                AutoLayout = AutoLayout.DockFill,
                Font = "font10",
                ItemHeight = 64,
                OnSelectedIndexChanged = (sender) =>
                {
                    if (sender is Gui.Widgets.WidgetListView list && list.SelectedIndex > 0 && list.SelectedItem.Tag is CreatureAI creature)
                    {
                        // Todo: Open employee window to that employee instead.
                        //right.Hidden = false;
                        //right.Employee = creature;
                    }
                    //else
                    //    right.Hidden = true;
                }
            }) as Gui.Widgets.WidgetListView;

            OnUpdate += (sender, time) =>
            {
                if (IsAnyParentHidden()) return;
                RebuildEmployeeList();
            };

            Root.RegisterForUpdate(this);

            RebuildEmployeeList();
        }
    }
}
