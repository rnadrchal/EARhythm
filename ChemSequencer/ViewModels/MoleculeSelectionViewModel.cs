using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Egami.Chemistry.Model;
using Egami.Chemistry.PubChem;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;

namespace ChemSequencer.ViewModels;

public class MoleculeSelectionViewModel : BindableBase, IDialogAware
{
    private readonly PubChemClient _pubChemClient;

    public ICommand CancelCommand { get; }
    public ICommand AcceptCommand { get; }
    public ICommand SearchCommand { get; }

    public string Title => "Select Molecule";

    private string _query;
    public string Query
    {
        get => _query;
        set => SetProperty(ref _query, value);
    }

    public List<int> _cids = new();
    public List<int> Cids
    {
        get => _cids;
        set => SetProperty(ref _cids, value);
    }

    public int? _cid;
    public int? SelectedCid
    {
        get => _cid;
        set => SetProperty(ref _cid, value);
    }

    private Molecule? _molecule;

    public MoleculeSelectionViewModel(PubChemClient pubChemClient)
    {
        _pubChemClient = pubChemClient;

        CancelCommand = new DelegateCommand(Cancel);
        SearchCommand = new DelegateCommand(async () => await Search());
        AcceptCommand = new DelegateCommand(Accept);
    }

    public bool CanCloseDialog()
    {
        return true;
    }

    public void OnDialogClosed()
    {
    }

    public void OnDialogOpened(IDialogParameters parameters)
    {
        
    }

    private void Cancel()
    {
        RaiseRequestClose(new DialogResult(ButtonResult.Cancel));
    }

    private void Accept()
    {
        if (Cids.Count > 0)
        {
            var result = new DialogResult(ButtonResult.OK);
            result.Parameters = new DialogParameters
            {
                { "SelectedCid", Cids.FirstOrDefault() },
                { "Molecule", _molecule }
            };
            RaiseRequestClose(result);
        }
    }

    private async Task Search()
    {
        Cids.Clear();
        try
        {
            var result = await _pubChemClient.SearchCidsByNameAsync(Query);
            Cids = result.ToList();
            if (result.Count > 0)
            {
                var cid = result.First();
                _molecule = await _pubChemClient.GetMoleculeAsync(cid);
            }
        }
        catch (HttpRequestException e)
        {
            Cids = new();
        }
    }

    public DialogCloseListener RequestClose { get; }

    public virtual void RaiseRequestClose(IDialogResult dialogResult)
    {
        RequestClose.Invoke(dialogResult);
    }
}