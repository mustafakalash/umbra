﻿/* Umbra | (c) 2024 by Una              ____ ___        ___.
 * Licensed under the terms of AGPL-3  |    |   \ _____ \_ |__ _______ _____
 *                                     |    |   //     \ | __ \\_  __ \\__  \
 * https://github.com/una-xiv/umbra    |    |  /|  Y Y  \| \_\ \|  | \/ / __ \_
 *                                     |______//__|_|  /____  /|__|   (____  /
 *     Umbra is free software: you can redistribute  \/     \/             \/
 *     it and/or modify it under the terms of the GNU Affero General Public
 *     License as published by the Free Software Foundation, either version 3
 *     of the License, or (at your option) any later version.
 *
 *     Umbra UI is distributed in the hope that it will be useful,
 *     but WITHOUT ANY WARRANTY; without even the implied warranty of
 *     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *     GNU Affero General Public License for more details.
 */

using System;
using System.Collections.Generic;
using System.Timers;
using Dalamud.Utility;
using Lumina.Excel.GeneratedSheets;
using Umbra.Common;
using Umbra.Widgets.System;

namespace Umbra.Widgets;

public sealed partial class CurrenciesWidget(
    WidgetInfo                  info,
    string?                     guid         = null,
    Dictionary<string, object>? configValues = null
) : DefaultToolbarWidget(info, guid, configValues)
{
    private readonly Timer _updateTimer           = new(1000);
    private          byte  _currentGrandCompanyId = 0;
    private          bool? _useGrayscaleIcon;

    /// <inheritdoc/>
    protected override void Initialize()
    {
        SyncTrackedCurrencyOptions();
        HydratePopupMenu();

        _updateTimer.Elapsed   += (_, _) => UpdateMenuItems();
        _updateTimer.AutoReset =  true;
        _updateTimer.Start();

        Node.OnClick += _ => UpdateMenuItems();
    }

    public override string GetInstanceName()
    {
        if (uint.TryParse(GetConfigValue<string>("TrackedCurrency"), out uint customId)) {
            if (CustomCurrencies.TryGetValue(customId, out Currency? customCurrency)) {
                return $"{I18N.Translate("Widget.Currencies.Name")} - {customCurrency.Name}";
            }

            return string.IsNullOrEmpty(GetConfigValue<string>("CustomLabel"))
                ? I18N.Translate("Widget.Currencies.Name")
                : GetConfigValue<string>("CustomLabel");
        }

        return GetConfigValue<string>("TrackedCurrency") != ""
            ? $"{I18N.Translate("Widget.Currencies.Name")} - {Currencies[Enum.Parse<CurrencyType>(GetConfigValue<string>("TrackedCurrency"))].Name}"
            : string.IsNullOrEmpty(GetConfigValue<string>("CustomLabel"))
                ? I18N.Translate("Widget.Currencies.Name")
                : GetConfigValue<string>("CustomLabel");
    }

    /// <inheritdoc/>
    protected override void OnUpdate()
    {
        SetGhost(!GetConfigValue<bool>("Decorate"));
        Popup.UseGrayscaleIcons = GetConfigValue<bool>("DesaturateIcons");

        UpdateCustomIdList();

        Node.QuerySelector("#Label")!.Style.TextOffset = new(0, GetConfigValue<int>("TextYOffset"));

        var trackedCurrencyId = GetConfigValue<string>("TrackedCurrency");
        var useGrayscaleIcon  = GetConfigValue<bool>("DesaturateIcon");

        if (uint.TryParse(GetConfigValue<string>("TrackedCurrency"), out uint customId)) {
            if (CustomCurrencies.TryGetValue(customId, out Currency? customCurrency)) {
                if (GetConfigValue<string>("IconLocation") == "Left") {
                    SetLeftIcon(GetConfigValue<bool>("ShowIcon") ? customCurrency.Icon : null);
                    SetRightIcon(null);
                } else {
                    SetLeftIcon(null);
                    SetRightIcon(GetConfigValue<bool>("ShowIcon") ? customCurrency.Icon : null);
                }

                if (_useGrayscaleIcon != useGrayscaleIcon) {
                    _useGrayscaleIcon = useGrayscaleIcon;

                    foreach (var node in Node.QuerySelectorAll(".icon")) {
                        node.Style.ImageGrayscale = useGrayscaleIcon;
                    }
                }

                string customName = GetConfigValue<bool>("ShowName") ? $" {customCurrency.Name}" : "";
                SetLabel($"{GetCustomAmount(customCurrency.Id)}{customName}");

                return;
            }
        }

        if (!Enum.TryParse(trackedCurrencyId, out CurrencyType currencyType) || currencyType == 0 || !Currencies.TryGetValue(currencyType, out Currency? currency)) {
            string customLabel = GetConfigValue<string>("CustomLabel");
            string label       = I18N.Translate("Widget.Currencies.Name");

            SetLabel(string.IsNullOrEmpty(customLabel) ? label : customLabel);
            SetLeftIcon(null);
            SetRightIcon(null);
            return;
        }

        if (GetConfigValue<string>("IconLocation") == "Left") {
            SetLeftIcon(GetConfigValue<bool>("ShowIcon") ? currency.Icon : null);
            SetRightIcon(null);
        } else {
            SetLeftIcon(null);
            SetRightIcon(GetConfigValue<bool>("ShowIcon") ? currency.Icon : null);
        }

        if (_useGrayscaleIcon != useGrayscaleIcon) {
            _useGrayscaleIcon = useGrayscaleIcon;

            foreach (var node in Node.QuerySelectorAll(".icon")) {
                node.Style.ImageGrayscale = useGrayscaleIcon;
            }
        }

        string name = GetConfigValue<bool>("ShowName") ? $" {currency.Name}" : "";
        SetLabel($"{GetAmount(currency.Type)}{name}");
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        _updateTimer.Stop();
        _updateTimer.Dispose();
    }

    private void UpdateMenuItems()
    {
        if (!Popup.IsOpen) return;

        byte gcId = Player.GrandCompanyId;

        if (gcId != _currentGrandCompanyId) {
            _currentGrandCompanyId = gcId;
            HydratePopupMenu();
            return;
        }

        foreach (var currency in Currencies.Values) {
            if (currency.Type == CurrencyType.Maelstrom && gcId != 1) continue;
            if (currency.Type == CurrencyType.TwinAdder && gcId != 2) continue;
            if (currency.Type == CurrencyType.ImmortalFlames && gcId != 3) continue;

            Popup.SetButtonAltLabel($"Currency_{currency.Id}", GetAmount(currency.Type));
        }
    }
}
