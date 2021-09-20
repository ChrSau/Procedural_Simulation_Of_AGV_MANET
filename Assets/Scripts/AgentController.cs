using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentController : MonoBehaviour{
    
    private Graph navGraph;
    private List<MobileAgent> AGVList = new List<MobileAgent>();
    private List<AccessPoint> APList = new List<AccessPoint>();
    private ResultLogger logger;

    public float taskDuration = 120.0f;
    private float timeTillTask = 0.0f;
    private bool isInitialized = false;
    private List<StatusMessage> loggedFeedbacks = new List<StatusMessage>();
    public List<int> connectedAGVs = new List<int>();

    public float connectionPercentage = 0.0f;

    public float logEvaluationTimeOut = 3.0f;
    private float timeTillLogEvaluation = 3.0f;

    public int completedTasks = 0;
    public float TphpAGV = 0.0f;
    
    private float timeOfLastFeedback;
    private int sourceOfLastFeedback;

    public bool maximumTaskLoad = false;

    private float startTime = 0.0f;

    void Start(){
        isInitialized = false;
        navGraph = GameObject.Find("NavGraph").GetComponent<Graph>();
        logger = GameObject.Find("ResultLogger").GetComponent<ResultLogger>();
    }

    void Update(){

        if(!maximumTaskLoad){
            timeTillTask -= Time.deltaTime;
            if(timeTillTask <= 0.0f && isInitialized){
                timeTillTask = taskDuration / AGVList.Count;
                newRandomTask();
            }
        }else{
            disseminateTasks();
        }

        timeTillLogEvaluation -= Time.deltaTime;
        if(timeTillLogEvaluation < 0.0f && isInitialized){
            evaluateLogs();
            timeTillLogEvaluation = logEvaluationTimeOut;
        }
    }

    private void disseminateTasks(){
        foreach(MobileAgent agv in AGVList){
            if(agv.state == MobileAgent.State.idle && connectedAGVs.Contains(agv.address)){
                TaskMessage tempMessage = new TaskMessage(agv.address, getRandomTaskDestination(), Random.Range(10.0f, 60.0f),false);
                sendMessage(tempMessage);
                break;
            }
        }
    }

    private void evaluateLogs(){
        evaluateConnectionLog();
        evaluatePerformance();
        toLogger();
    }

    private void toLogger(){
        string tempLog = "";
        tempLog += (Time.time - startTime).ToString();
        tempLog += ";";
        tempLog += connectionPercentage.ToString();
        tempLog += ";";
        tempLog += TphpAGV.ToString();
        logger.log(tempLog);
    }

    public void setStartTime(){
        startTime = Time.time;
    }

    public float getCurrentSimTime(){
        return Time.time - startTime;
    }

    private void evaluatePerformance(){
        float transportsPerSecond = completedTasks / getCurrentSimTime();
        float transportsPerHour = transportsPerSecond * 3600.0f;
        TphpAGV = transportsPerHour / AGVList.Count;

    }

    private void evaluateConnectionLog(){
        int numberOfAGVs = AGVList.Count;
        connectedAGVs.Clear();
        foreach(StatusMessage feedback in loggedFeedbacks){
            if(!connectedAGVs.Contains(feedback.sender) && feedback.isConnected){
                connectedAGVs.Add(feedback.sender);
            }
        }
        int numberOfConnectedAGVs = connectedAGVs.Count;
        loggedFeedbacks.Clear();
        connectionPercentage = ((float)(numberOfConnectedAGVs) / (float)(numberOfAGVs)) * 100.0f;
        //Debug.Log(connectionPercentage.ToString() + "% of all AGVs are connected.");
    }

    private void newRandomTask(){
        MobileAgent taskRecipient = getRandomTaskRecipient();
        GraphNode taskDestination = getRandomTaskDestination();
        float taskDuration = Random.Range(10.0f,60.0f);

        TaskMessage taskMessage = new TaskMessage(taskRecipient.address, taskDestination, taskDuration);
        //Debug.Log("New Task: AGV " + taskMessage.recipient + " must drive to (" + taskMessage.destination.position.x + "; " + taskMessage.destination.position.z + ") and wait for " + taskDuration.ToString() + "s.");
        sendMessage(taskMessage);
    }

    private GraphNode getRandomTaskDestination(){
        List<GraphNode> possibleDestinations = navGraph.getNodesOfType(GraphNode.Type.task);
        possibleDestinations.AddRange(navGraph.getNodesOfType(GraphNode.Type.parking));
        GraphNode destination = possibleDestinations[Random.Range(0, possibleDestinations.Count - 1)];
        while(graphNodeIsOccupied(destination)){
            destination = possibleDestinations[Random.Range(0, possibleDestinations.Count - 1)];
        }
        return destination;
    }

    private MobileAgent getRandomTaskRecipient(){
        foreach(MobileAgent agv in AGVList){
            if(agv.state == MobileAgent.State.idle && connectedAGVs.Contains(agv.address)){
                return agv;
            }
        }

        return AGVList[Random.Range(0, AGVList.Count - 1)];
    }

    private void sendMessage(Message _message){
        foreach(AccessPoint AP in APList){
            AP.sendMessage(_message);
        }
    }

    private bool graphNodeIsOccupied(GraphNode _node){
        bool isOccupied = false;
        foreach(MobileAgent agv in AGVList){
            if(Vector3.Distance(agv.gameObject.transform.position, _node.position) < 1.0f){
                isOccupied = true;
                break;
            }
        }
        return isOccupied;
    }

    public void clear(){
        AGVList.Clear();
        APList.Clear();
        AGVList.TrimExcess();
        APList.TrimExcess();
        isInitialized = false;
        startTime = Time.time;
        completedTasks = 0;
    }

    public void initialize(){

        Transform AGVGroup = GameObject.Find("MobileAgentGroup").transform;
        for(int i=0; i<AGVGroup.childCount; i++){
            AGVList.Add(AGVGroup.GetChild(i).gameObject.GetComponent<MobileAgent>());
        }

        Transform APGroup = GameObject.Find("APGroup").transform;
        for(int i=0; i<APGroup.childCount; i++){
            APList.Add(APGroup.GetChild(i).gameObject.GetComponent<AccessPoint>());
        }

        timeTillTask = taskDuration / AGVList.Count;

        isInitialized = true;
    }

    public void receiveMessage(Message _message){
        switch (_message.type){
            case Message.Type.status: handleFeedback(_message as StatusMessage); break;
            default: break;
        }
    }

    private void handleFeedback(StatusMessage _statusMessage){
        float timeSinceLastFeedback = Time.time - timeOfLastFeedback;
        if(timeSinceLastFeedback > 1.0f || sourceOfLastFeedback != _statusMessage.sender){
            loggedFeedbacks.Add(_statusMessage);
            completedTasks += _statusMessage.numberOfCompletedTasks;
            StatusAcknowledgeMessage acknoledgement = new StatusAcknowledgeMessage(_statusMessage.sender);
            sendMessage(acknoledgement);
            timeOfLastFeedback = Time.time;
            sourceOfLastFeedback = _statusMessage.sender;
        }
    }
}
