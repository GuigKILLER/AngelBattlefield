﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab.ClientModels;
using PlayFab;
using System;


namespace KILLER
{
    public class PlayFabManager : MonoBehaviour
    {
        
        public static PlayFabManager instance = null;
        public GetCharacterLeaderboardResult players;
        public GetTitleNewsResult news;
        TMPro.TMP_Text txtMessage;
        public GameObject panel, settingsWindow;
        GameObject player;
        [SerializeField]
        int LoadingTimeOut = 3;

        public string[] keysTab;
        public string Player_ID, Player_DisplayName;
        public int Player_Lvl, Player_Gold, Player_PPID, Player_TitleID, Player_Xp;
        public List<ItemInstance> inv;
        public List<StoreItem> PPStore, TitleStore, SkinStore;
        public List<List<StoreItem>> store;
        void Awake()
        {

            //Singleton
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
            DontDestroyOnLoad(gameObject);
            store = new List<List<StoreItem>>() { { PPStore }, { TitleStore }, { SkinStore } };

            panel = transform.Find("CanvasLoading").Find("Panel").gameObject;
            txtMessage = panel.GetComponentInChildren<TMPro.TMP_Text>();

        }

        #region UTILS

        public void LoadingHide()
        {
            StartCoroutine(Timer());
        }
        IEnumerator Timer()
        {
            yield return new WaitForSeconds(LoadingTimeOut);
            panel.SetActive(false);
        }
        public void LoadingMessage(string msg)
        {
            LoadingShow();
            txtMessage.text = msg;
        }
        public void LoadingShow()
        {
            if (!panel.activeInHierarchy)
            {
                panel.SetActive(true);
            }
        }
        void DisplayPlayFabError(PlayFabError error)
        {
            LoadingMessage(error.ErrorMessage);
            print(error.ErrorDetails);
            LoadingHide();
        }
        public List<PlayFab.ClientModels.FriendInfo> friendList;
        public void GetFriends()
        {
            PlayFabClientAPI.GetFriendsList(new GetFriendsListRequest
            {
                ProfileConstraints = new PlayerProfileViewConstraints
                {
                    ShowStatistics = true,
                    ShowDisplayName = true
                }
            }, result =>
            {
                friendList = result.Friends;
            },
                DisplayPlayFabError
            );
        }
        public void RemoveFriend(string FriendID)
        {
            PlayFabClientAPI.RemoveFriend(new RemoveFriendRequest
            {
                FriendPlayFabId = FriendID
            }, result =>
            {
            },
                DisplayPlayFabError
            );
            GetFriends();
        }
        public void ADDFriend()
        {

            PlayFabClientAPI.AddFriend(new AddFriendRequest
            {
                FriendUsername = GameObject.Find("FriendSC").GetComponent<TMPro.TMP_InputField>().text
            }, result =>
            {

            },
                DisplayPlayFabError
            ) ;
            GetFriends();
        }
        public void ReportPlayer(string ID)
        {
            PlayFabClientAPI.ReportPlayer(new ReportPlayerClientRequest
            {
                ReporteeId = ID,
                Comment = "Reported"
            }, result =>
            {
                print("Reported");
            },
                DisplayPlayFabError
            );
        }

        public void GetCurencies(Action action)
        {
            PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest
            { }, result =>
            {
                foreach (var item in result.VirtualCurrency)
                {
                    if (item.Key == "GO")
                    {
                        Player_Gold = item.Value;
                    }
                }
                action();
            },errorCallback =>
            {
                LoadingMessage(errorCallback.Error.ToString());
                LoadingHide();
            }
            );
        }

        public void GetUserData(string searchedKey,int index)
        {
            PlayFabClientAPI.GetUserData(new GetUserDataRequest()
            {}, result => 
            {
                if (result.Data == null)
                {

                }
                else if (!result.Data.ContainsKey(searchedKey))
                {
                    Debug.Log("No " + searchedKey);
                    SetUserData(searchedKey, keysTab[index], index, delegate { GetUserData(searchedKey, index); });
                }
                else
                {
                    keysTab[index] = result.Data[searchedKey].Value;
                }
            }, (error) => 
            {
                Debug.Log("Got error retrieving user data:");
                Debug.Log(error.GenerateErrorReport());
            });
        }

        public void SetUserData(string data, string value, int index, Action action = null)
        {
            PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest()
            {
                Data = new Dictionary<string, string>() {
                    {data, value}
                }
            },
            result =>
            {
                keysTab[index] = value;
                print("DataSaved");
                action();
            },
            error => {
                Debug.Log("Got error setting user data Ancestor to Arthur");
                Debug.Log(error.GenerateErrorReport());
            });
        }

        public void UseCode(string code, Action action)
        {
            PlayFabClientAPI.RedeemCoupon(new RedeemCouponRequest
            { 
                CouponCode = code,
                CatalogVersion = "PP"
            }, result =>
            {
                LoadingMessage("Code Used");
                LoadingHide();
                action();
            }, errorCallback =>
            {
                LoadingMessage(errorCallback.Error.ToString());
                LoadingHide();
            }
            );
        }

        public void GetStore(string ID, int storePlace, Action action, string catalog)
        {
            PlayFabClientAPI.GetStoreItems(new GetStoreItemsRequest
            {
                StoreId = ID,
                CatalogVersion = catalog
            }, result =>
            {
                print("StoreLoaded");
                store[storePlace] = new List<StoreItem>(result.Store);
                action();
            },errorCallback =>
            {
                DisplayPlayFabError(errorCallback);
            }
            );;
        }
        public void ReadTitleNews(Action action)
        {
            PlayFabClientAPI.GetTitleNews(new GetTitleNewsRequest(), result => {
                news = result;
                action();
                // Process news using result.News
            }, error => Debug.LogError(error.GenerateErrorReport()));
        }
        public void GetInventory()
        {
            PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), LogSuccess, LogFailure);
        }

        private void LogSuccess(GetUserInventoryResult obj)
        {
            inv = obj.Inventory;
        }

        private void LogFailure(PlayFabError obj)
        {
            throw new NotImplementedException();
        }

        public void AddXp(int xp)
        {
            Player_Xp += xp;
            while(Player_Xp >= 2000)
            {
                Player_Xp -= 2000;
                Player_Lvl++;
            }
            StartCoroutine(SaveWithDelay());
        }

        public void MakePurchaseWithVC(string ID, string catalogID, int price)
        {
            bool test = true;
            foreach(ItemInstance item in inv)
            {
                if(item.ItemId == ID && item.CatalogVersion == catalogID)
                {
                    test = false;
                    break;
                }
            }
            if (test)
            {
                PlayFabClientAPI.PurchaseItem(new PurchaseItemRequest
                {
                    ItemId = ID,
                    VirtualCurrency = "GO",
                    CatalogVersion = catalogID,
                    Price = price
                }, result =>
                {
                    GetInventory();
                    LoadingMessage("Purchase succes");
                    LoadingHide();
                }, errorCallback =>
                {
                    DisplayPlayFabError(errorCallback);
                }
                );
            }
            else
            {
                LoadingMessage("Already Bought");
                LoadingHide();
            }
        }

        #endregion

        #region SAVE

        public IEnumerator SaveWithDelay()
        {
            yield return new WaitForSeconds(0.3f);
            save();
        }
        public void save()
        {
            //ReadStatScore2();
            //LoadingMessage("save...");
            player = GameObject.Find("Player");
            var request = new UpdatePlayerStatisticsRequest
            {
                Statistics = new List<StatisticUpdate>
                {
                    new StatisticUpdate {StatisticName = "xp", Value = Player_Xp}
                }
            };
            PlayFabClientAPI.UpdatePlayerStatistics(request, Success, Failed);
        }


        private void Success(UpdatePlayerStatisticsResult obj)
        {
            //LoadingHide();
            save1();
        }
        private void save1()
        {
            //LoadingMessage("save xp...");

            var request = new UpdatePlayerStatisticsRequest
            {
                Statistics = new List<StatisticUpdate>
                {
                    new StatisticUpdate {StatisticName = "level", Value = Player_Lvl}
                }
            };
            PlayFabClientAPI.UpdatePlayerStatistics(request, Success1, Failed);
        }


        private void Success1(UpdatePlayerStatisticsResult obj)
        {
            //LoadingHide();
            save2();
        }

        private void save2()
        {
            //LoadingMessage("save xp...");

            var request = new UpdatePlayerStatisticsRequest
            {
                Statistics = new List<StatisticUpdate>
                {
                    new StatisticUpdate {StatisticName = "PpId", Value = Player_PPID}
                }
            };
            PlayFabClientAPI.UpdatePlayerStatistics(request, Success2, Failed);
        }


        private void Success2(UpdatePlayerStatisticsResult obj)
        {
            //LoadingHide();
            ReadStatScore();
        }

        public void saveRoom()
        {
            //LoadingMessage("save xp...");

            var request = new UpdatePlayerStatisticsRequest
            {
                Statistics = new List<StatisticUpdate>
                {
                    new StatisticUpdate {StatisticName = "room", Value = Convert.ToInt32(PhotonNetwork.room.Name)}
                }
            };
            PlayFabClientAPI.UpdatePlayerStatistics(request, SuccessRoom, Failed);
        }


        private void SuccessRoom(UpdatePlayerStatisticsResult obj)
        {
            //LoadingHide();
            ReadStatScore();
        }

        public void ReadStatScore()
        {
            var request = new GetPlayerStatisticsRequest();
            PlayFabClientAPI.GetPlayerStatistics(request, SuccessStat, FailedStat);
        }

        private void FailedStat(PlayFabError err)
        {
            LoadingMessage(err.ErrorMessage);
            LoadingHide();
        }
        private void SuccessStat(GetPlayerStatisticsResult result)
        {
            foreach (var item in result.Statistics)
            {
                print(result.Statistics);
                if (item.StatisticName == "xp")
                    Player_Xp = item.Value;
                if (item.StatisticName == "level")
                    Player_Lvl = item.Value;
                if (item.StatisticName == "PpId")
                    Player_PPID = item.Value;
            }
        }
        private void Failed(PlayFabError err)
        {
            LoadingMessage(err.ErrorMessage);
            LoadingHide();
        }

        #endregion

        #region Quit

        private void OnApplicationQuit()
        {
            LoadingMessage("Save room...");

            var request = new UpdatePlayerStatisticsRequest
            {
                Statistics = new List<StatisticUpdate>
                {
                    new StatisticUpdate {StatisticName = "room", Value = 0}
                }
            };
            PlayFabClientAPI.UpdatePlayerStatistics(request, SuccessRoomQuit, Failed);
        }
        private void SuccessRoomQuit(UpdatePlayerStatisticsResult obj)
        {
            LoadingMessage("Quit...");
        }

        #endregion

        #region PAYPAL

        public void MakePurchase(string ID)
        {
            PlayFabClientAPI.StartPurchase(new StartPurchaseRequest()
            {
                Items = new List<ItemPurchaseRequest>() {
                new ItemPurchaseRequest() {
                    ItemId = ID,
                    Quantity = 1,
                    Annotation = "Purchased via in-game store"
                }
            }
            }, result =>
            {
                // Handle success
                ContinuePurchase(result.OrderId);
            }, error =>
            {
                LoadingMessage(error.ErrorMessage);
                LoadingHide();
                // Handle error
            }); 
        }
        public void ContinuePurchase(string ID)
        {
            PlayFabClientAPI.PayForPurchase(new PayForPurchaseRequest()
            {
                OrderId = ID,
                ProviderName = "PayPal",
                Currency = "RM"
            }, result => {
                Application.OpenURL(result.PurchaseConfirmationPageURL);
                // Handle success
                FinishPurchase(ID);
            }, error => {
                LoadingMessage(error.ErrorMessage);
                LoadingHide();
                // Handle error
            });
        }
        public void FinishPurchase(string ID)
        {
            PlayFabClientAPI.ConfirmPurchase(new ConfirmPurchaseRequest()
            {
                OrderId = ID
            }, result => {
                LoadingMessage("Achat terminé");
                LoadingHide();
                // Handle success
            }, error => {
                // Handle error
                LoadingMessage(error.ErrorMessage);
                LoadingHide();
            });
        }
        #endregion
    }
}
