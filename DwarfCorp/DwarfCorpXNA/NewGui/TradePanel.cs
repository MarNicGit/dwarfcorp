﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;
using Microsoft.Xna.Framework;

namespace DwarfCorp.NewGui
{
    public interface ITradeEntity
    {
        List<ResourceAmount> Resources { get; }
        int Money { get; }
    }

    public class TradePanel : Widget
    {
        public ITradeEntity Player;
        public ITradeEntity Envoy;
        private ResourceColumns PlayerColumns;
        private ResourceColumns EnvoyColumns;
        private Widget TotalDisplay;

        public override void Construct()
        {
            Border = "border-fancy";

            var bottomRow = AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockBottom,
                MinimumSize = new Point(0, 32)
            });

            TotalDisplay = bottomRow.AddChild(new Widget
            {
                MinimumSize = new Point(128, 0),
                AutoLayout = AutoLayout.DockLeft,
                Text = "$0"
            });

            var mainPanel = AddChild(new TwoColumns
            {
                AutoLayout = AutoLayout.DockFill
            });

            EnvoyColumns = mainPanel.AddChild(new ResourceColumns
            {
                SourceResources = Envoy.Resources,
                AutoLayout = AutoLayout.DockFill,
                LeftHeader = "Their Items",
                RightHeader = "They Offer",
                Money = Envoy.Money,
                OnTotalSelectedChanged = (s) => UpdateTotalDisplay()
            }) as ResourceColumns;

            PlayerColumns = mainPanel.AddChild(new ResourceColumns
            {
                SourceResources = Player.Resources,
                AutoLayout = AutoLayout.DockFill,
                ReverseColumnOrder = true,
                LeftHeader = "Out Items",
                RightHeader = "We Offer",
                Money = Player.Money,
                OnTotalSelectedChanged = (s) => UpdateTotalDisplay()
            }) as ResourceColumns;
            
        }        

        private void UpdateTotalDisplay()
        {
            var net = EnvoyColumns.TotalSelectedValue - PlayerColumns.TotalSelectedValue;
            TotalDisplay.Text = String.Format("${0}", net);
            TotalDisplay.Invalidate();
        }
    }
}
