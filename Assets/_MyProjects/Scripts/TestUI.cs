using System;
using UnityEngine;
using UnityEngine.UI;

public class TestUI : MonoBehaviour
{
    public static TestUI Instance;
    [SerializeField] private Button mintTestTokensButton;
    [SerializeField] private Button purchaseTestNFTButton;
    [SerializeField] private Button printInventoryButton;

    private void Awake()
    {
        Instance = this;
        ManageButtons(false);
    }

    private void OnEnable()
    {
        mintTestTokensButton.onClick.AddListener(MintTestTokens);
        purchaseTestNFTButton.onClick.AddListener(PurchaseTestNFT);
        printInventoryButton.onClick.AddListener(PrintInventory);
    }

    private void OnDisable()
    {
        mintTestTokensButton.onClick.RemoveListener(MintTestTokens);
        purchaseTestNFTButton.onClick.RemoveListener(PurchaseTestNFT);
        printInventoryButton.onClick.RemoveListener(PrintInventory);
    }

    private void MintTestTokens()
    {
        ManageButtons(false);
        BoomDaoManager.Instance.MintIcrc(() =>
        {
            ManageButtons(true);
        });
    }

    private void PurchaseTestNFT()
    {
        ManageButtons(false);
        BoomDaoManager.Instance.BuyWithIcrc(BoomDaoManager.BUY_ITEM_B_ICRC, () =>
        {
            ManageButtons(true);
        });
    }

    private void PrintInventory()
    {
        if (BoomDaoManager.Instance.GetInventory().Count==0)
        {
            CustomLogger.Log("There are no items in inventory");
            return;
        }
        
        foreach (var (_,_value) in BoomDaoManager.Instance.GetInventory())
        {
            if (_value.gid != BoomDaoManager.ITEM_TAG)
            {
                continue;
            }
            
            try
            {
                if (!EntityUtil.GetConfigFieldAs<string>(_value.GetConfigId(), "name", out var _configName))
                {
                    throw new Exception($"Element of id : \"{_value.GetConfigId()}\" doesn't have field \"item\"");
                }
                if (!_value.GetFieldAs<double>("quantity", out var _currentQuantity))
                {
                    throw new Exception($"Element of id : \"{_value.GetKey()}\" doesn't have field \"quantity\"");
                }

                CustomLogger.Log($"{_configName} x {_currentQuantity}");
            }
            catch (Exception _err)
            {
                Debug.LogError(_err.Message);
            }
        }
    }

    public void ManageButtons(bool _status)
    {
        mintTestTokensButton.interactable = _status;
        purchaseTestNFTButton.interactable = _status;
        printInventoryButton.interactable = _status;
    }
}
