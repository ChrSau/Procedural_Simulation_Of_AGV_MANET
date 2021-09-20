using System.Collections;
using System.Collections.Generic;
//using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class SimulationController : MonoBehaviour
{
    private SimulationGenerator simulationGenerator;
    private AgentController agentController;
    private Graph navGraph;
    private Text startButtonUiText;
    private Medium medium;
    private ResultLogger logger;
    private ConnectionGraph connectionGraph;

    public bool isRunning = true;
    public float timeTillNew = 0.0f;
    private bool newIsDone = false;

    private float timeScaleSetting = 1.0f;

    private float timeTillReconfiguration = 0.0f;
    public float reconfigurationTimeout = 200.0f;

    public bool reconfiguring = false;
    public float disabledAPsPercentage = 0.5f;
    private bool alreadyReconfigured = false;
    public int currentSeed;


    void Awake(){
        simulationGenerator = GameObject.Find("SimulationGenerator").GetComponent<SimulationGenerator>();
        agentController = GameObject.Find("AGVController").GetComponent<AgentController>();
        navGraph = GameObject.Find("NavGraph").GetComponent<Graph>();
        startButtonUiText = GameObject.Find("Start-Button").transform.GetChild(0).GetComponent<Text>();
        medium = GameObject.Find("Medium").GetComponent<Medium>();
        logger = GameObject.Find("ResultLogger").GetComponent<ResultLogger>();
        connectionGraph = GameObject.Find("ConnectionGraph").GetComponent<ConnectionGraph>();
        toggleSimulationTime();
        timeTillReconfiguration = reconfigurationTimeout;
    }
    void Start(){
        newSimulation();
    }

    void Update(){
        if(!newIsDone){
        timeTillNew -= Time.unscaledDeltaTime;
            if(timeTillNew < 0){
                makeNew();
            }
        }

        GameObject.Find("TimeScale-Text").GetComponent<Text>().text = timeScaleSetting.ToString();
        if(isRunning){
            Time.timeScale = timeScaleSetting;
        }

        if(reconfiguring && newIsDone){
            if(timeTillReconfiguration < 0.0f && !alreadyReconfigured && simulationGenerator.generationDone){
                simulationGenerator.reconfigure(disabledAPsPercentage);
                timeTillReconfiguration = Mathf.Infinity;
                alreadyReconfigured = true;
            }else{
                timeTillReconfiguration -= Time.deltaTime;
            }
        }

        if(newIsDone && GameObject.Find("MultiSimulationManager").GetComponent<MultiSimulationManager>().active){
            if(!isRunning){
                toggleSimulationTime();
            }
        }
    }

    public void makeNew(){
        int seed = (int)(System.DateTime.Now.Ticks);
        //int seed = -1933246571;
        currentSeed = seed;
        simulationGenerator.newConfiguration(seed);
        agentController.initialize();
        medium.initialize();
        enableAllAPs();
        newIsDone = true;
        alreadyReconfigured = false;
        timeTillReconfiguration = reconfigurationTimeout;
    }

    private void enableAllAPs(){
        Transform APGroup = GameObject.Find("APGroup").transform;
        for(int i=0; i < APGroup.childCount; i++){
            APGroup.GetChild(i).GetComponent<AccessPoint>().working = true;
        }
    }

    public void newSimulation(){
        if(isRunning){
            toggleSimulationTime();
        }
        navGraph.clear();
        deleteAllEntities();
        agentController.clear();
        logger.resetLog();
        simulationGenerator.StopAllCoroutines();

        timeTillNew = 1.0f;
        newIsDone = false;
    }

    /*private void clearLog(){
        var assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
        var type = assembly.GetType("UnityEditor.LogEntries");
        var method = type.GetMethod("Clear");
        method.Invoke(new object(), null);
    }*/

    private void deleteAllEntities(){
        GameObject mobileAgentGroup = GameObject.Find("MobileAgentGroup");
        for(int i=0; i < mobileAgentGroup.transform.childCount; i++){
            Destroy(mobileAgentGroup.transform.GetChild(i).gameObject);
        }
        GameObject APGroup = GameObject.Find("APGroup");
        for(int i=0; i < APGroup.transform.childCount; i++){
            Destroy(APGroup.transform.GetChild(i).gameObject);
        }
        GameObject ObstacleGroup = GameObject.Find("ObstacleGroup");
        for(int i=0; i < ObstacleGroup.transform.childCount; i++){
            Destroy(ObstacleGroup.transform.GetChild(i).gameObject);
        }
    }

    public void toggleSimulationTime(){
        if(isRunning){
            Time.timeScale = 0.0f;
            startButtonUiText.text = "Start Simulation";
            isRunning = false;
        }else{
            Time.timeScale = timeScaleSetting;
            startButtonUiText.text = "Stop Simulation";
            isRunning = true;
        }
    }

    public void lowerTimeScale(){
        if(Time.timeScale >= 1.1f){
            timeScaleSetting = timeScaleSetting - 0.1f;
        }
    }

    public void raiseTimeScale(){
        if(Time.timeScale <= 5.0f){
            timeScaleSetting = timeScaleSetting + 0.1f;
        }
    }

}
