using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RatioMaster.App.ViewModels.Dialogs;

/// <summary>One button in a message dialog.</summary>
public sealed record DialogButton(string Label, string Result, bool IsDefault = false, bool IsCancel = false);
