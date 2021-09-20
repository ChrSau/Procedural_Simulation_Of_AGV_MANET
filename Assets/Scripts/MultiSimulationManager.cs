using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiSimulationManager : MonoBehaviour{
    public enum ManagerState{
        Starting,
        Running,
        Regenerating
    }

    private AgentController agvController;
    private SimulationGenerator simGenerator;
    private SimulationController simController;
    private ResultLogger logger;
    public ManagerState state = ManagerState.Starting;
    private bool regenerationDone = true;

    public bool active = false;
    public float simulationDuration = 100.0f;
    public int timeScaleRaises = 2;
    private int currentSeed;
    private int runCounter = 0;
    public int startCounter = 0;

    void Awake(){
        agvController = GameObject.Find("AGVController").GetComponent<AgentController>();
        simGenerator = GameObject.Find("SimulationGenerator").GetComponent<SimulationGenerator>();
        simController = GameObject.Find("SimulationController").GetComponent<SimulationController>();
        logger = GameObject.Find("ResultLogger").GetComponent<ResultLogger>();
    }

    void Start(){
        if(!active){
            return;
        }
        for(int i = 0; i < timeScaleRaises; i++){
            simController.raiseTimeScale();
        }
    }

    void Update(){
        if(!active){
            return;
        }
        switch (state){
            case ManagerState.Starting:
                if(simGenerator.generationDone){
                    agvController.setStartTime();
                    simController.toggleSimulationTime();
                    state = ManagerState.Running;
                }
                break;
            case ManagerState.Running:
                checkPerformance();
                if(agvController.getCurrentSimTime() > simulationDuration){
                    simController.toggleSimulationTime();
                    saveLog();
                    StartCoroutine(regenerationTimer());
                    float diceThrow = Random.Range(0.0f, 1.0f);
                    if(diceThrow < 0.33f){
                        simGenerator.AGVsAreAdHoc = false;
                        simGenerator.AGVsAreAdaptive = false;
                    }else if(diceThrow > 0.66f){
                        simGenerator.AGVsAreAdHoc = true;
                        simGenerator.AGVsAreAdaptive = false;
                    }else{
                        simGenerator.AGVsAreAdHoc = true;
                        simGenerator.AGVsAreAdaptive = true;
                    }
                    simController.newSimulation();
                    currentSeed = simController.currentSeed;
                    state = ManagerState.Regenerating;
                }
                break;
            case ManagerState.Regenerating:
                if(simGenerator.generationDone && regenerationDone){
                    state = ManagerState.Starting;
                    runCounter++;
                }
                break;
            default:
                Debug.Log("MulitSimulationManager has state error");
                break;
        }
    }

    private void checkPerformance(){
        float lastFPS = 1.0f / Time.deltaTime;
        if(lastFPS > 50){
            simController.raiseTimeScale();
        }
        if(lastFPS < 20){
            simController.lowerTimeScale();
        }
        //Debug.Log(Time.timeScale);
    }

    private void saveLog(){
        string fileName = "SF_" + (runCounter + startCounter).ToString() + ".csv";
        //string fileName = "Assets/lastLogs/SF_" + (runCounter + startCounter).ToString() + ".csv";
        Debug.Log("Saving as " + fileName);
        logger.saveToFile(fileName);
    }

    IEnumerator regenerationTimer(){
        regenerationDone = false;
        yield return new WaitForSecondsRealtime(5);
        regenerationDone = true;
    }
}
