using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class ResultLogger : MonoBehaviour{

    public List<string> logStrings = new List<string>();

    public Text stringDisplay;

    void Update(){
        displayLastEntries(30);
    }

    private void displayLastEntries(int _numberOfEntriesToDisplay){
        string subString = "";
        int numberOfEntries = logStrings.Count;
        int startIndex = numberOfEntries > _numberOfEntriesToDisplay ? numberOfEntries - _numberOfEntriesToDisplay : 0;
        for(int i = startIndex; i < numberOfEntries; i++){
            subString += logStrings[i];
            subString += "\n";
        }
        stringDisplay.text = subString;
    }

    public void log(string _input){
        logStrings.Add(_input);
        Debug.Log(_input);
    }

    public void resetLog(){
        logStrings.Clear();
    }

    public void copyLogToClipboard(){
        string assambled = assambleLogString();
        TextEditor te = new TextEditor();
        te.text = assambled;
        te.SelectAll();
        te.Copy();
    }

    private string assambleLogString(){
        string assambled = "";
        int numberOfEntries = logStrings.Count;
        for(int i = 0; i < numberOfEntries; i++){
            assambled += logStrings[i];
            assambled += "\n";
        }
        return assambled;
    }

    public void saveToFile(string _path){
        StreamWriter writer = new StreamWriter(_path, true);
        writer.Write(assambleLogString());
        writer.Close();
    }

}
