using System.Collections;
using System.Collections.Generic;
using Candid;
using UnityEngine;
using Boom;
using Boom.Patterns.Broadcasts;
using Boom.Utility;
using Boom.Values;
using Candid.World.Models;
using Cysharp.Threading.Tasks;
using Action = System.Action;

public class BoomDaoManager : MonoBehaviour
{
    public static BoomDaoManager Instance;

    public const string ITEM_TAG = "item";
    public const string BUY_ITEM_B_ICRC = "buyItemB_Icrc";

    [SerializeField] private string url;
    private DataState<Data<DataTypes.Entity>> someData;

    private void Awake()
    {
        if (Instance)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void Login(Action _callBack)
    {
        CustomLogger.Log("Started logging in");
        LoginManager.Instance.SetUlr(url);
        StartCoroutine(LoginRoutine());
        
        IEnumerator LoginRoutine()
        {
            yield return new WaitUntil(()=>CandidApiManager.Instance.CanLogIn);
            CandidApiManager.Instance.OnLoginFinished.AddListener(FinishLogin);
            UserUtil.StartLogin("Logging in...");       
        }

        void FinishLogin()
        {
            CustomLogger.Log($"Got principal id: {UserUtil.GetPrincipal()}");
            RegisterToChanges(_callBack);
        }
    }
    
    private void RegisterToChanges(Action _callBack)
    {
        UserUtil.RegisterToDataChange<DataTypes.Entity>((_data) =>
        {
            CollectSomeData(_data,_callBack);
        }, true);
    }
    
    private void CollectSomeData(DataState<Data<DataTypes.Entity>> _state, Action _callBack)
    {
        if (_state.IsNull())
        {
            //Nothing in your inventory
            _callBack?.Invoke();
            return;
        }

        if (_state.IsLoading())
        {
            //Loading inventory...
            return;
        }

        someData = _state;
        _callBack?.Invoke();
    }

    public Dictionary<string, DataTypes.Entity> GetInventory()
    {
        if (someData==default)
        {
            return new Dictionary<string, DataTypes.Entity>();
        }

        return someData.data.elements;
    }

    public async UniTaskVoid MintIcrc(Action _callBack)
    {
        string _mintIcrcActionId = "mint_test_icrc";
        BroadcastState.Invoke(new WaitingForResponse(true));
        var _actionResult = await ActionUtil.Action.Default(_mintIcrcActionId);

        if (_actionResult.Tag == UResultTag.Err)
        {
            CustomLogger.Log(_actionResult.AsErr().Content, Color.red);
            BroadcastState.Invoke(new WaitingForResponse(false));
            return;
        }

        var _resultAsOk = _actionResult.AsOk();
        var _actionConfigResult = UserUtil.GetElementOfType<DataTypes.Action>(_mintIcrcActionId);

        if (_actionConfigResult.IsErr)
        {
            CustomLogger.Log(_actionResult.AsErr().Content, Color.red);
            BroadcastState.Invoke(new WaitingForResponse(false));
            return;
        }

        BroadcastState.Invoke(new WaitingForResponse(false));

        string _output = $"Mint ICRC Success, Wait for approval, reward {_resultAsOk.tokens.Count}:\n\n " +
                         $"{_resultAsOk.tokens.Reduce(_e => $"Token canister: {_e.Canister}\nQuantity: {_e.Quantity}\n\n")}";
        CustomLogger.Log(_output);
        _callBack?.Invoke();
    }

    public async UniTaskVoid BuyWithIcrc(string _actionId, Action _callBack)
    {
        var _offers = GetValidOffers(new List<string> {_actionId});
        ActionPlugin _actionPluginResult=default;
        foreach (var _offer in _offers)
        {
            _actionPluginResult = _offer.value.actionPlugin;
        }

        if (_actionPluginResult==default)
        {
            CustomLogger.Log("Invalid action id: "+_actionId);
            _callBack?.Invoke();
            return;
        }
        var _config = _actionPluginResult.AsVerifyTransferIcrc();

        BroadcastState.Invoke(new WaitingForResponse(true));
        var _tokenSymbol = "ICRC";
        var _userBalance = 0D;
        var _tokenAndConfigsResult = TokenUtil.GetTokenDetails(_config.Canister);
        if (_tokenAndConfigsResult.Tag == UResultTag.Ok)
        {
            var (_token, _tokenConfigs) = _tokenAndConfigsResult.AsOk();
            _tokenSymbol = _tokenConfigs.symbol;
            _userBalance = _token.baseUnitAmount.ConvertToDecimal(_tokenConfigs.decimals);
        }

        CustomLogger.Log( $"Required ICRC: {_config.Amt} Balance: {_userBalance}");

        var _actionResult = await ActionUtil.Action.TransferAndVerifyIcrc(_actionId, _config.Canister);

        if (_actionResult.Tag == UResultTag.Err)
        {
            var _actionError = _actionResult.AsErr();

            switch (_actionError)
            {
                case ActionErrType.InsufficientBalance:
                    CustomLogger.Log($"You don't have enough {_tokenSymbol}\nRequirements:\n{_tokenSymbol} x {_config.Amt}\nYou need to mint more \"{_tokenSymbol}\"");
                    break;
                case ActionErrType.ActionsPerInterval _content:
                    CustomLogger.Log($"Time constrain!\n{_content.Content}");
                    break;
                default:
                    CustomLogger.Log("Other issue! " + _actionResult.AsErr().GetType().Name);
                    CustomLogger.Log( _actionResult.AsErr().content);
                    break;
            }

            BroadcastState.Invoke(new WaitingForResponse(false));
            _callBack?.Invoke();
            return;
        }

        BroadcastState.Invoke(new WaitingForResponse(false));
        CustomLogger.Log( $"Successfully purchased: {_actionId}", Color.green);
        _callBack?.Invoke();
    }

    private List<KeyValue<string, DataTypes.Action>> GetValidOffers(List<string> _actionOfferIds)
    {
        List<KeyValue<string, DataTypes.Action>> _offers = new();
        _actionOfferIds.Iterate(_e =>
        {
            var _getDataResult = UserUtil.GetElementOfType<DataTypes.Action>(_e);
            if (_getDataResult.Tag == UResultTag.Err)
            {
                return;
            }

            if (_getDataResult.Tag == UResultTag.None)
            {
                return;
            }

            var _config = _getDataResult.AsOk();

            var _actionPlugin = _config.actionPlugin;

            if (_actionPlugin != null)
            {
                if (_actionPlugin.Tag != ActionPluginTag.VerifyTransferIcp &&
                    _actionPlugin.Tag != ActionPluginTag.VerifyTransferIcrc &&
                    _actionPlugin.Tag != ActionPluginTag.VerifyBurnNfts) return;
            }

            _offers.Add(new(_e, _config));
        });

        return _offers;
    }
}
