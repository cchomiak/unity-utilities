﻿// C# example:
using UnityEngine;
using UnityEditor;

using System.Collections;
using System.Collections.Generic;

using Microsoft.Win32;


//using System;
//using System.Diagnostics;
//using System.IO;
//using System.Xml;

public enum SortingType { Ascending, Descending };

public class PlayerPrefsEditor2 : EditorWindow
{
    string myString = "Hello World";
    bool groupEnabled;
    bool myBool = true;
    
    float myFloat = 1.23f;

    List<PlayerPrefsValue> playerPrefs;
    Dictionary<PlayerPrefsValue, PlayerPrefsValue> originalPrefs;

    private Vector2 scrollPosition;

    private bool somethingWasDeleted;
    private string searchString = "";
    private SortingType sortingType = SortingType.Ascending;
    private SortingType prevSortingType;

    private Texture2D undoTexture, saveTexture, deleteTexture;

    private List<PlayerPrefsValue> toBeDeleted;

    private int updateFactor = 100;
    private int currentFrame = 0;

    private int currentNumberOfUpdates = 0;
    private int maxUpdatesPerSecond = 1;

    private bool autoRefresh = false;
    bool foldout = true;

    bool createNewPref = false;

    PlayerPrefsValue newPlayerPref = null;
    bool incorrectKeyName = false;
    bool duplicateKeyName = false;

    /*private string[] sortingOptions = new string[] { "Sort (A-Z)", "Rev. Sort (Z-A)" };
    private int selectedSortingOption = 0;
    private int prevSelectedSortingOption = 0;*/

    private bool sortIsAZ = true;
    private int prevKeyNameSize = 0;

    // Add menu named "My Window" to the Window menu
    [MenuItem("Window/PlayerPrefs Editor (2)")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        PlayerPrefsEditor2 window = (PlayerPrefsEditor2) EditorWindow.GetWindow(typeof(PlayerPrefsEditor2));
        //window.Show();
        //window.somethingWasDeleted = false;
    }

    void OnEnable()
    {
    }

    void OnDisable()
    {
        //EditorPrefs.SetBool("myBool", myBool);
        //Debug.Log("Salvando: " + EditorPrefs.GetBool("myBool"));
    }

    void OnDestroy()
    {
    }

    void SavePlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
        foreach (PlayerPrefsValue pref in playerPrefs)
            pref.SaveToRealPrefs();
        
        RefreshPlayerPrefs();
    }

    void OnInspectorUpdate()
    {
        if (autoRefresh)
        {
            if (currentFrame == 0)
            {
                RefreshPlayerPrefs();
                Debug.Log("Updated");
            }
            Debug.Log("Current: " + currentFrame);

            currentFrame++;
            currentFrame %= 10;

            /*if (currentFrame >= updateFactor)
            {
                RefreshPlayerPrefs();
                Debug.Log("Update :D " + currentFrame);
                currentFrame = 0;
            }*/
        }
    }

    void Update()
    {
        /*currentFrame++;

        if (currentFrame >= updateFactor)
        {
            RefreshPlayerPrefs();
            Debug.Log("Update :D " + currentFrame);
            currentFrame = 0;
        }*/
    }

    void SortAZ()
    {
        playerPrefs.Sort(delegate(PlayerPrefsValue a, PlayerPrefsValue b)
                            {
                                return a.keyName.CompareTo(b.keyName);
                            }
                        );
        sortIsAZ = true;
    }

    void SortZA()
    {
        SortAZ();
        playerPrefs.Reverse();
        sortIsAZ = false;
    }

    void DeleteAll()
    {
        PlayerPrefs.DeleteAll();
        RefreshPlayerPrefs();
    }
    
    void DeleteSelected()
    {
       List<PlayerPrefsValue> toBeDeleted = new List<PlayerPrefsValue>();
       foreach(PlayerPrefsValue ppv in playerPrefs)
       {
           if (ppv.isSelected)
           {
               PlayerPrefs.DeleteKey(ppv.keyName);
               //toBeDeleted.Add(ppv);
               //originalPrefs.Remove(ppv);
           }
       }
       //playerPrefs.RemoveAll(x => toBeDeleted.Contains(x));
       RefreshPlayerPrefs();
    }

    void SelectAll()
    {
        foreach (PlayerPrefsValue ppv in playerPrefs)
        {
            ppv.isSelected = true;
        }
    }

    void DeselectAll()
    {
        foreach (PlayerPrefsValue ppv in playerPrefs)
        {
            ppv.isSelected = false;
        }
    }

    void InverseSelection()
    {
        foreach (PlayerPrefsValue ppv in playerPrefs)
        {
            ppv.isSelected = !ppv.isSelected;
        }
    }

    void UndoSelected()
    {
        foreach (PlayerPrefsValue ppv in playerPrefs)
        {
            if (ppv.isSelected)
            {
                ppv.CopyFrom(originalPrefs[ppv]);
                ppv.isSelected = false;
            }
        }
        GUI.FocusControl(null);
    }

    void UndoAll()
    {
        foreach (PlayerPrefsValue ppv in playerPrefs)
        {
            ppv.CopyFrom(originalPrefs[ppv]);
            ppv.isSelected = false;
        }
        GUI.FocusControl(null);
    }

    void SaveAll()
    {
        foreach (PlayerPrefsValue ppv in playerPrefs)
        {
            ppv.SaveToRealPrefs();
            ppv.isSelected = false;
        }
        RefreshPlayerPrefs();
    }

    void SaveSelected()
    {
        foreach (PlayerPrefsValue ppv in playerPrefs)
        {
            if (ppv.isSelected)
            {
                ppv.SaveToRealPrefs();
                ppv.isSelected = false;
            }
        }
    }

    bool CheckDuplicates(PlayerPrefsValue ppv)
    {
        if (ppv.prevKeyNameLength != ppv.keyName.Length)
        {
            //duplicateKeyName = false;
            //Debug.Log("Chequeo duplicados");
            foreach (PlayerPrefsValue otherPPV in playerPrefs)
            {
                if (ppv == otherPPV)
                    continue;

                if (otherPPV.keyName == ppv.keyName)
                {
                    //duplicateKeyName = true;
                    //break;
                    return true;
                }
            }
            ppv.prevKeyNameLength = ppv.keyName.Length;
        }
        return false;
    }

    private bool IsEditor
    {
        get { return !Application.isPlaying; }
    }

    void OnGUI()
    {
        /*if (!Application.isPlaying)
            Debug.Log("Editor");
        else //if (!Application.isPlaying)
            Debug.Log("Playing");*/

        if (playerPrefs == null)
            RefreshPlayerPrefs();

        if (toBeDeleted == null)
            toBeDeleted = new List<PlayerPrefsValue>();
        else
            toBeDeleted.Clear();

        if (undoTexture == null)
        {
            undoTexture = AssetDatabase.LoadAssetAtPath("Assets/Editor/EditorIcons/undo.png", typeof(Texture2D)) as Texture2D;
        }
        if (deleteTexture == null)
        {
            deleteTexture = AssetDatabase.LoadAssetAtPath("Assets/Editor/EditorIcons/delete.png", typeof(Texture2D)) as Texture2D;
        }
        if (saveTexture == null)
        {
            saveTexture = AssetDatabase.LoadAssetAtPath("Assets/Editor/EditorIcons/save.png", typeof(Texture2D)) as Texture2D;
        }
        
        #region Toolbar

        GUILayout.BeginHorizontal(GUI.skin.FindStyle("Toolbar"));

        /*string[] options = new string[]{ "New Pref",  "Delete All" };
        optionsIndex = EditorGUILayout.Popup("Options", optionsIndex, options, EditorStyles.toolbarPopup);
        if (GUILayout.Button("Options"))
            Debug.Log("Opcion: " + optionsIndex);*/
        /*EditorGUILayout.Popup(0, options);
        )*/

        /*if (GUILayout.Button(new GUIContent("Delete All"), EditorStyles.toolbarButton, GUILayout.Width(64)))
        {
            PlayerPrefs.DeleteAll();
            RefreshPlayerPrefs();
            return;
        }*/

        if (IsEditor)
        {
            if (GUILayout.Button(new GUIContent("New Pref"), EditorStyles.toolbarButton, GUILayout.Width(64)))
            {
                newPlayerPref = new PlayerPrefsValue("", PlayerPrefsTypes.Int, 0);
                createNewPref = true;
                incorrectKeyName = false;
            }
        }

        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("Sort (A-Z)"), sortIsAZ, new GenericMenu.MenuFunction(SortAZ));
        menu.AddItem(new GUIContent("Reverse Sort (Z-A)"), !sortIsAZ, new GenericMenu.MenuFunction(SortZA));
        //menu.ShowAsContext();

        if (GUILayout.Button("Sort", EditorStyles.toolbarDropDown))
        {
            menu.ShowAsContext();
        }

        menu = new GenericMenu();
        menu.AddItem(new GUIContent("All"), false, new GenericMenu.MenuFunction(SelectAll));
        menu.AddItem(new GUIContent("None"), false, new GenericMenu.MenuFunction(DeselectAll));
        menu.AddItem(new GUIContent("Inverse"), false, new GenericMenu.MenuFunction(InverseSelection));
        //menu.ShowAsContext();

        if (GUILayout.Button("Select", EditorStyles.toolbarDropDown))
        {
            menu.ShowAsContext();
        }

        menu = new GenericMenu();
        menu.AddItem(new GUIContent("Save Selected"), false, new GenericMenu.MenuFunction(SaveSelected));
        menu.AddItem(new GUIContent("Save All"), false, new GenericMenu.MenuFunction(SaveAll));
        menu.AddItem(new GUIContent("Undo Selected"), false, new GenericMenu.MenuFunction(UndoSelected));
        menu.AddItem(new GUIContent("Undo All"), false, new GenericMenu.MenuFunction(UndoAll));
        menu.AddItem(new GUIContent("Delete Selected"), false, new GenericMenu.MenuFunction(DeleteSelected));
        menu.AddItem(new GUIContent("Delete All"), false, new GenericMenu.MenuFunction(DeleteAll));

        //menu.ShowAsContext();

        if (GUILayout.Button("Options", EditorStyles.toolbarDropDown))
        {
            menu.ShowAsContext();
        }

        /*menu = new GenericMenu();
        menu.AddItem(new GUIContent("Selected"), false, new GenericMenu.MenuFunction(SaveSelected));
        menu.AddItem(new GUIContent("All"), false, new GenericMenu.MenuFunction(SaveAll));
        //menu.ShowAsContext();

        if (GUILayout.Button("Save", EditorStyles.toolbarDropDown))
        {
            menu.ShowAsContext();
        }

        menu = new GenericMenu();
        menu.AddItem(new GUIContent("Selected"), false, new GenericMenu.MenuFunction(DeleteSelected));
        menu.AddItem(new GUIContent("All"), false, new GenericMenu.MenuFunction(DeleteAll));
        //menu.ShowAsContext();

        if (GUILayout.Button("Delete", EditorStyles.toolbarDropDown))
        {
            menu.ShowAsContext();
        }*/

        //selectedSortingOption = EditorGUILayout.Popup(selectedSortingOption, sortingOptions, EditorStyles.toolbarPopup, GUILayout.MaxWidth(100));
        //sortingType = (SortingType)EditorGUILayout.EnumPopup(sortingType, EditorStyles.toolbarDropDown, GUILayout.MaxWidth(80));

        GUILayout.FlexibleSpace();
        searchString = GUILayout.TextField(searchString, GUI.skin.FindStyle("ToolbarSeachTextField"), GUILayout.MaxWidth(100));
        if (GUILayout.Button("", GUI.skin.FindStyle("ToolbarSeachCancelButton")))
        {
            // Remove focus if cleared
            searchString = "";
            GUI.FocusControl(null);
        }


        if (!IsEditor)
            autoRefresh = GUILayout.Toggle(autoRefresh, "Auto-refresh");
        else
            autoRefresh = false;
        

        if (GUILayout.Button(new GUIContent("Reload"), EditorStyles.toolbarButton, GUILayout.Width(56)))
        {
            RefreshPlayerPrefs();
            return;
        }

        GUILayout.EndHorizontal();
        
        #endregion


        #region NewPref

        if (createNewPref && newPlayerPref != null)
        {
            GUI.skin.font = EditorStyles.boldFont;
            EditorGUILayout.LabelField("New Preference", EditorStyles.boldLabel); //, EditorStyles.boldLabel);
            GUI.skin.font = EditorStyles.standardFont;

            EditorGUILayout.BeginVertical();

                EditorGUILayout.BeginHorizontal();                
                    GUILayout.Space(25);
                    EditorGUILayout.LabelField("Key", GUILayout.MaxWidth(36));
                    newPlayerPref.keyName = EditorGUILayout.TextField(newPlayerPref.keyName, GUILayout.MaxWidth(128)); //, GUILayout.MinWidth(75), GUILayout.MaxWidth(100));
                EditorGUILayout.EndHorizontal();

                if (newPlayerPref.keyName == "")
                {
                    incorrectKeyName = true;
                }
                else
                {
                    incorrectKeyName = false;

                    /*if (prevKeyNameSize != newPlayerPref.keyName.Length)
                    {
                        duplicateKeyName = false;
                        //Debug.Log("Chequeo duplicados");
                        foreach (PlayerPrefsValue ppv in playerPrefs)
                        {
                            if (ppv.keyName == newPlayerPref.keyName)
                            {
                                duplicateKeyName = true;
                                break;
                            }
                        }
                        prevKeyNameSize = newPlayerPref.keyName.Length;
                    }*/
                    //duplicateKeyName = CheckDuplicates();
                    duplicateKeyName = CheckDuplicates(newPlayerPref);
                }

                EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(25);
                    EditorGUILayout.LabelField("Value", GUILayout.MaxWidth(36));
                    switch (newPlayerPref.valueType)
                    {
                        case PlayerPrefsTypes.Int:
                            newPlayerPref.intValue = EditorGUILayout.IntField(newPlayerPref.intValue, EditorStyles.textField, GUILayout.MaxWidth(128));
                            break;
                        case PlayerPrefsTypes.Float:
                            newPlayerPref.floatValue = EditorGUILayout.FloatField(newPlayerPref.floatValue, EditorStyles.textField, GUILayout.MaxWidth(128));
                            break;
                        case PlayerPrefsTypes.String:
                            newPlayerPref.stringValue = EditorGUILayout.TextField(newPlayerPref.stringValue, EditorStyles.textField, GUILayout.MaxWidth(128));
                            break;
                        default:
                            newPlayerPref.stringValue = EditorGUILayout.TextField(newPlayerPref.stringValue, EditorStyles.textField, GUILayout.MaxWidth(128));
                            break;
                    }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(25);
                    EditorGUILayout.LabelField("Type", GUILayout.MaxWidth(36));
                    newPlayerPref.valueType = (PlayerPrefsTypes)EditorGUILayout.EnumPopup(newPlayerPref.valueType, EditorStyles.toolbarPopup, GUILayout.MaxWidth(128));
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(10);
                if (incorrectKeyName)
                    EditorGUILayout.HelpBox("Error: Invalid name", MessageType.Error, true);
                else if (duplicateKeyName)
                    EditorGUILayout.HelpBox("Warning: Duplicate name found. 'Create' will overwrite the existing key.", MessageType.Warning, true);
            
                EditorGUILayout.BeginHorizontal();

                GUI.enabled = !incorrectKeyName;
                if (GUILayout.Button(new GUIContent("Create"))) //, GUILayout.MaxHeight(24), GUILayout.Width(24)))
                {    
                    incorrectKeyName = false;
                    
                    newPlayerPref.SaveToRealPrefs();
                    RefreshPlayerPrefs();
                    /*playerPrefs.Add(newPlayerPref);
                    RefreshPlayerPrefs();*/

                    createNewPref = false;
                    newPlayerPref = null;
                    GUI.FocusControl(null);
                    return;

                    /*originalPrefs[pref].CopyFrom(pref);
                    GUI.FocusControl(null);
                    Debug.Log("Saved!!");*/
                }
                GUI.enabled = true;

                if (GUILayout.Button(new GUIContent("Cancel"))) //, GUILayout.MaxHeight(24), GUILayout.Width(24)))
                {
                    createNewPref = false;
                    newPlayerPref = null;
                    GUI.FocusControl(null);
                }
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(16);

                //sortingType = (SortingType)EditorGUILayout.EnumPopup(sortingType, EditorStyles.toolbarDropDown, GUILayout.MaxWidth(80));

                EditorGUILayout.EndVertical();

        }

        #endregion


        #region PreferencesList

        GUI.skin.font = EditorStyles.boldFont;
        foldout = EditorGUILayout.Foldout(foldout, "Player Preferences"); //, EditorStyles.boldLabel);
        GUI.skin.font = EditorStyles.standardFont;
        
        if (foldout)
        {
            //GUILayout.Label("Player Preferences", EditorStyles.boldLabel);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            if (playerPrefs.Count == 0)
            {
                //GUILayout.Label("Currently there are no PlayerPrefs", EditorStyles.miniLabel);
                EditorGUILayout.HelpBox("No Player Preferences found", MessageType.Info);
            }
            else
            {
                /*if (prevSelectedSortingOption != selectedSortingOption)
                {
                    playerPrefs.Sort(delegate(PlayerPrefsValue a, PlayerPrefsValue b)
                                        {
                                            return a.keyName.CompareTo(b.keyName);
                                        }
                     );

                    if (selectedSortingOption != 0)
                    {
                        playerPrefs.Reverse();
                    }
                }
                prevSelectedSortingOption = selectedSortingOption;*/

                /*if (sortingType != prevSortingType)
                {
                    if (sortingType == SortingType.Ascending)
                    {
                        playerPrefs.Sort(delegate(PlayerPrefsValue a, PlayerPrefsValue b)
                        {
                            return a.keyName.CompareTo(b.keyName);
                        }
                        );
                    }
                    else
                    {
                        playerPrefs.Sort(delegate(PlayerPrefsValue a, PlayerPrefsValue b)
                        {
                            return a.keyName.CompareTo(b.keyName);
                        }
                        );
                        playerPrefs.Reverse();
                    }
                }
                prevSortingType = sortingType;*/

                bool atLeastOneSearchMatched = false;
                foreach (PlayerPrefsValue pref in playerPrefs)
                {
                    if (!pref.keyName.ToLowerInvariant().Contains(searchString))
                        continue;

                    atLeastOneSearchMatched = true;
                    GUILayout.BeginHorizontal();

                    pref.isSelected = EditorGUILayout.Toggle(pref.isSelected, GUILayout.MaxWidth(16));

                    //GUILayout.Label(pref.keyName, GUILayout.MinWidth(75), GUILayout.MaxWidth(100));

                    if (pref.IsDifferent(originalPrefs[pref]))
                        GUI.skin.font = EditorStyles.boldFont;
                    else
                        GUI.skin.font = EditorStyles.standardFont;

                    if (IsEditor)
                        pref.keyName = GUILayout.TextField(pref.keyName, GUILayout.MinWidth(75), GUILayout.MaxWidth(100));
                    else
                        GUILayout.Label(pref.keyName, EditorStyles.textField, GUILayout.MinWidth(75), GUILayout.MaxWidth(100));

                    switch (pref.valueType)
                    {
                        case PlayerPrefsTypes.Int:
                            if (IsEditor)
                                pref.intValue = EditorGUILayout.IntField(pref.intValue, EditorStyles.textField, GUILayout.MaxWidth(150));
                            else
                                GUILayout.Label(pref.intValue.ToString(), EditorStyles.textField, GUILayout.MinWidth(75), GUILayout.MaxWidth(100));
                            break;
                        case PlayerPrefsTypes.Float:
                            if (IsEditor)
                                pref.floatValue = EditorGUILayout.FloatField(pref.floatValue, EditorStyles.textField, GUILayout.MaxWidth(150));
                            else
                                GUILayout.Label(pref.floatValue.ToString(), EditorStyles.textField, GUILayout.MinWidth(75), GUILayout.MaxWidth(100));
                            break;
                        case PlayerPrefsTypes.String:
                            if (IsEditor)
                                pref.stringValue = EditorGUILayout.TextField(pref.stringValue, EditorStyles.textField, GUILayout.MaxWidth(150));
                            else
                                GUILayout.Label(pref.stringValue, EditorStyles.textField, GUILayout.MinWidth(75), GUILayout.MaxWidth(100));
                            break;
                        default:
                            if (IsEditor)
                                pref.stringValue = EditorGUILayout.TextField(pref.stringValue, EditorStyles.textField, GUILayout.MaxWidth(150));
                            else
                                GUILayout.Label(pref.stringValue, EditorStyles.textField, GUILayout.MinWidth(75), GUILayout.MaxWidth(100));
                            break;
                    }

                    if (IsEditor)
                        pref.valueType = (PlayerPrefsTypes)EditorGUILayout.EnumPopup(pref.valueType, EditorStyles.toolbarPopup, GUILayout.MaxWidth(64));
                    else
                        GUILayout.Label(pref.valueType.ToString(), EditorStyles.textField, GUILayout.MinWidth(75), GUILayout.MaxWidth(100));

                    int iconSize = 28;

                    GUILayout.Space(8);

                    if (IsEditor)
                    {

                        GUI.enabled = pref.keyName.Length != 0 && !CheckDuplicates(pref);
                        if (GUILayout.Button(new GUIContent(saveTexture, "Save this preference"), EditorStyles.toolbarButton, GUILayout.Height(iconSize), GUILayout.Width(iconSize)))
                        {
                            if (pref.keyName != originalPrefs[pref].keyName)
                            {
                                PlayerPrefs.DeleteKey(originalPrefs[pref].keyName);
                            }

                            originalPrefs[pref].CopyFrom(pref);
                            GUI.FocusControl(null);

                            pref.SaveToRealPrefs();
                            //Debug.Log("Saved!!");
                        }
                        GUI.enabled = true;

                        if (GUILayout.Button(new GUIContent(undoTexture, "Revert this preference"), EditorStyles.toolbarButton, GUILayout.Height(iconSize), GUILayout.Width(iconSize)))
                        {
                            //revertThisPref = pref;
                            pref.CopyFrom(originalPrefs[pref]);
                            GUI.FocusControl(null);
                            //Debug.Log("Restored!!");
                        }
                        else if (GUILayout.Button(new GUIContent(deleteTexture, "Delete this preference"), EditorStyles.toolbarButton, GUILayout.Height(iconSize), GUILayout.Width(iconSize)))
                        {
                            pref.toBeDeleted = true;
                            toBeDeleted.Add(pref);
                            //somethingWasDeleted = true;
                        }
                    }
                    GUILayout.EndHorizontal();
                    GUI.skin.font = EditorStyles.standardFont;
                }
                if (!atLeastOneSearchMatched)
                {
                    EditorGUILayout.HelpBox("No matches for that search.", MessageType.Info);                     
                }

                foreach (PlayerPrefsValue pref in toBeDeleted)
                {
                    if (pref.toBeDeleted)
                    {
                        playerPrefs.Remove(pref);
                        originalPrefs.Remove(pref);
                        PlayerPrefs.DeleteKey(pref.keyName);
                    }
                }
                /*if (toBeDeleted.Count != 0)
                {
                    SavePlayerPrefs();
                }*/


                /*if (GUI.changed)
                {
                    EditorUtility.SetDirty(this);
                }*/
            }
            GUILayout.EndScrollView();

        }
        #endregion



        /*GUILayout.Label("Base Settings", EditorStyles.boldLabel);
        myString = EditorGUILayout.TextField("Text Field", myString);

        groupEnabled = EditorGUILayout.BeginToggleGroup("Optional Settings", groupEnabled);
        myBool = EditorGUILayout.Toggle("Toggle", myBool);
        myFloat = EditorGUILayout.Slider("Slider", myFloat, -3, 3);
        EditorGUILayout.EndToggleGroup();*/

    }

    void RefreshPlayerPrefs()
    {
        somethingWasDeleted = false;

        if (playerPrefs == null)
            playerPrefs = new List<PlayerPrefsValue>();
        else
            playerPrefs.Clear();

        if (originalPrefs == null)
            originalPrefs = new Dictionary<PlayerPrefsValue, PlayerPrefsValue>();
        else
            originalPrefs.Clear();


        if (Application.platform == RuntimePlatform.WindowsEditor)
        {
            GetPlayerPrefsWindows();
        }
        else if (Application.platform == RuntimePlatform.OSXEditor)
        {
            Debug.Log("Coming soon");
        }

        if (sortIsAZ)
            SortAZ();
        else
            SortZA();
    }

    void GetPlayerPrefsWindows()
    {
        //In Windows, Unity stores the PlayerPrefs in the Registry, in a key identifiable by the Company and Product name of the game.
        string regKey = @"Software\" + PlayerSettings.companyName + @"\" + PlayerSettings.productName;

        RegistryKey key = Registry.CurrentUser.OpenSubKey(regKey);

        if (key == null)
        {
            Debug.Log("No keys found in registry.");
            return;
        }

        foreach (string subkeyName in key.GetValueNames())
        {
            string keyName = subkeyName.Substring(0, subkeyName.LastIndexOf("_"));
            string val = key.GetValue(subkeyName).ToString();
            
            int testInt = -1;
            bool couldBeInt = int.TryParse(val, out testInt);

            if (!float.IsNaN(PlayerPrefs.GetFloat(keyName, float.NaN)))
            {
                PlayerPrefsValue newPref = new PlayerPrefsValue(keyName, PlayerPrefsTypes.Float, PlayerPrefs.GetFloat(keyName));
                playerPrefs.Add(newPref);
                originalPrefs.Add(newPref, new PlayerPrefsValue(keyName, PlayerPrefsTypes.Float, PlayerPrefs.GetFloat(keyName))); 
            }
            else if (couldBeInt && (PlayerPrefs.GetInt(keyName, testInt - 10) == testInt))
            {
                PlayerPrefsValue newPref = new PlayerPrefsValue(keyName, PlayerPrefsTypes.Int, PlayerPrefs.GetInt(keyName));
                playerPrefs.Add(newPref);
                originalPrefs.Add(newPref, new PlayerPrefsValue(keyName, PlayerPrefsTypes.Int, PlayerPrefs.GetInt(keyName)));
            }
            else
            {
                PlayerPrefsValue newPref = new PlayerPrefsValue(keyName, PlayerPrefsTypes.String, PlayerPrefs.GetString(keyName));
                playerPrefs.Add(newPref);
                originalPrefs.Add(newPref, new PlayerPrefsValue(keyName, PlayerPrefsTypes.String, PlayerPrefs.GetString(keyName)));
            }
        }
    }

}