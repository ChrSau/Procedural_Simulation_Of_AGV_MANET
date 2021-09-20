using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobileAgent : MonoBehaviour{

    public enum State{
        idle,
        driving,
        waiting
    }
    
    public int address;
    public List<TaskMessage> taskBackLog = new List<TaskMessage>();
    public State state = State.idle;
    public bool isConnected = false;

    public Color freeColor = Color.black;
    public Color drivingColor = Color.cyan;
    public Color waitingColor = Color.magenta;

    public List<Vector3> route = new List<Vector3>();
    public float destinationTolerance = 0.1f;
    public float speed = 0.3f;
    public float feedbackTimeout = 3.0f;
    private float timeTillFeedback = 3.0f;

    private float timeTillAssistRequest = 0.0f;
    private float assistRequestTimeout = 60.0f;

    public bool adHocNetwork = false;
    public bool adaptive = false;

    private int tasksSinceLastFeedback = 0;

    private int unAcknowledgedStatus = 0;
    private List<Vector3> requestedAssistanceRoute = new List<Vector3>();
    private float assistanceRouteDisplayTimeout = 0.0f;

    private List<RelayBidMessage> bidBuffer = new List<RelayBidMessage>();
    private List<GraphNode> requestedRoute = new List<GraphNode>();


    private Graph navGraph;
    private Medium medium;
    private ConnectionKnowledgeBase ckb;
    private RelayAssitance relayassistant;

    private ResultLogger logger;

    void Start(){
        navGraph = GameObject.Find("NavGraph").GetComponent<Graph>();
        medium = GameObject.Find("Medium").GetComponent<Medium>();
        ckb = transform.gameObject.GetComponent<ConnectionKnowledgeBase>();
        relayassistant = transform.gameObject.GetComponent<RelayAssitance>();
        logger = GameObject.Find("ResultLogger").GetComponent<ResultLogger>();
        timeTillFeedback = Random.Range(0.0f, feedbackTimeout);
        bidBuffer.Clear();
    }

    void Update(){
        fulfillTask();
        timeTillFeedback -= Time.deltaTime;
        if(timeTillFeedback < 0.0f){
            timeTillFeedback = feedbackTimeout;
            sendFeedback();
            sendExchange();
        }

        isConnected = unAcknowledgedStatus < 3;

        ckb.registerMoment(transform.position, isConnected);
        requestAssistance();
    }

    private void sendExchange(){
        KnowledgeExchangeMessage exchangeMessage = ckb.getRandomExchangeMessage();
        if(exchangeMessage != null){
            send(exchangeMessage);
        }
    }

    private void requestAssistance(){
        if(!adaptive || !adHocNetwork){
            return;
        }

        if(isConnected){
            timeTillAssistRequest = assistRequestTimeout;
        }
        timeTillAssistRequest -= Time.deltaTime;

        if(timeTillAssistRequest < 0.0f){
            determineAssistanceRoute(navGraph.getClosestNode(transform.position));
            timeTillAssistRequest = assistRequestTimeout;
        }
    }

    private void sendFeedback(){
        StatusMessage statusMessage = new StatusMessage(state, address, transform.position, isConnected, tasksSinceLastFeedback);
        send(statusMessage);
        tasksSinceLastFeedback = 0;
        unAcknowledgedStatus++;

        /*string logString = "";
        logString += transform.position.x + ";";
        logString += transform.position.z + ";";
        logString += isConnected ? "1" : "0";
        logger.log(logString);*/
    }

    private void fulfillTask(){
        if(taskBackLog.Count > 0){
            if(Vector3.Distance(transform.position, taskBackLog[0].destination.position) < destinationTolerance){
                if(taskBackLog[0].waitTime > 0.0f){
                    taskBackLog[0].waitTime -= Time.deltaTime;
                    state = State.waiting;
                }else{
                    bool wasRelayTask = taskBackLog[0].isRelayTask;
                    taskBackLog.RemoveAt(0);
                    route.Clear();
                    if(!wasRelayTask){
                        tasksSinceLastFeedback += 1;
                    }
                }
            }else{
                state = State.driving;
                drive();
            }
        }else{
            state = State.idle;
        }
        
    }

    private void setRoute(){
        route.Clear();
        foreach(GraphNode node in navGraph.getRoute(navGraph.getClosestNode(transform.position), taskBackLog[0].destination)){
            route.Add(node.position);
        }
    }

    private void drive(){
        if(route.Count != 0){
            if(Vector3.Distance(route[0], transform.position) < destinationTolerance){
                route.RemoveAt(0);
            }else{
                driveToPoint(route[0]);
            }
        }else if(taskBackLog.Count > 0){
            setRoute();

            if(!adaptive || !adHocNetwork){
                return;
            }
            float conProb = ckb.getConnectionProbability(taskBackLog[0].destination.position);
            if(conProb < 0.85f){
                determineAssistanceRoute(taskBackLog[0].destination);
            }
        }
    }

    private void driveToPoint(Vector3 _point){
        Vector3 movement = (_point - transform.position);
        movement = movement.normalized;
        movement = movement * speed * Time.deltaTime;
        transform.position = transform.position + movement;
    }

    public void receiveMessage(Message _message){

        if(_message.recipient == address){
            handleMessage(_message);
        }else if(_message.recipient == 0){
            handleMessage(_message);
            routeMessage(_message);
        }else{
            routeMessage(_message);
        }
    }

    private void handleMessage(Message _message){
        if(_message.type == Message.Type.task){
            handleTaskMessage(_message as TaskMessage);
        }else if(_message.type == Message.Type.acknowledge){
            unAcknowledgedStatus = 0;
        }else if(_message.type == Message.Type.knowledgeExchange){
            ckb.incorporate(_message as KnowledgeExchangeMessage);
        }else if(_message.type == Message.Type.relayOffer){
            handleRelayOffer(_message as RelayOfferMessage);
        }else if(_message.type == Message.Type.relayBid){
            if(_message.recipient == address && !bidBuffer.Contains(_message as RelayBidMessage)){
                bidBuffer.Add(_message as RelayBidMessage);
            }
        }
    }

    private void handleTaskMessage(TaskMessage _task){
        if(!similarTaskKnown(_task)){
            if(taskBackLog.Count > 0){
                if(taskBackLog[0].isRelayTask){
                    taskBackLog.RemoveAt(0);
                    route.Clear();
                }
            }
            taskBackLog.Add(_task);
        }
    }

    private void handleRelayOffer(RelayOfferMessage _offer){
        if(state == State.idle){
            float distanceToRelayPoint = Vector3.Distance(_offer.relayPosition.position, transform.position);
            RelayBidMessage bid = new RelayBidMessage(address, _offer.sender, distanceToRelayPoint, _offer.relayPosition);
            send(bid);
        }
    }

    private void determineAssistanceRoute(GraphNode _destination){
        List<GraphNode> route = relayassistant.getAssistanceRoute(_destination);
        if(route.Count == 0){
            assistanceRouteDisplayTimeout = 0.0f;
            requestedAssistanceRoute.Clear();
            return;
        }
        int routeHopCount = route.Count + 1;
        //Debug.Log("New route of " + routeHopCount + " hops is deterined.");
        assistanceRouteDisplayTimeout = 60.0f;
        requestedAssistanceRoute.Clear();
        float closestAPDistance = Mathf.Infinity;
        Vector3 closestAPPosition = Vector3.zero;
        for(int i = 0; i < GameObject.Find("APGroup").transform.childCount; i++){
            Vector3 currentAPPosition = GameObject.Find("APGroup").transform.GetChild(i).position;
            float currentDistance = Vector3.Distance(currentAPPosition, _destination.position);
            if(currentDistance < closestAPDistance){
                closestAPDistance = currentDistance;
                closestAPPosition = currentAPPosition;
            }
        }
        requestedAssistanceRoute.Add(closestAPPosition);
        
        foreach(GraphNode node in route){
            requestedAssistanceRoute.Add(node.position);
        }
        requestedAssistanceRoute.Add(_destination.position);

        bidBuffer.Clear();
        foreach(GraphNode relayPosition in route){
            requestRelay(relayPosition);
            requestedRoute.Add(relayPosition);
        }
        StartCoroutine(evaluateRelayBids());
    }

    private IEnumerator evaluateRelayBids(){
        yield return new WaitForSeconds(5.0f);

        //evaluate
        //Debug.Log("Got bids by " + bidBuffer.Count + " biders.");

        foreach(GraphNode relayNode in requestedRoute){
            int bestBidder = -1;
            float bestBid = Mathf.Infinity;
            foreach(RelayBidMessage bid in bidBuffer){
                if(bid.bidFor == relayNode){
                    if(bid.distanceToRelayPosition < bestBid){
                        bestBid = bid.distanceToRelayPosition;
                        bestBidder = bid.sender;
                    }
                }
            }
            TaskMessage relayTask = new TaskMessage(bestBidder, relayNode, 60.0f, true);
            send(relayTask);
            //Debug.Log(bestBidder + " was the best choice for the node (" + relayNode.position.x.ToString() + "|" + relayNode.position.z.ToString() + ").");
        }

        bidBuffer.Clear();
        requestedRoute.Clear();
    }

    private void requestRelay(GraphNode _relayPosition){
        RelayOfferMessage offerMessage = new RelayOfferMessage(address, _relayPosition);
        send(offerMessage);
    }

    private bool similarTaskKnown(TaskMessage _task){
        foreach(TaskMessage task in taskBackLog){
            if(task.destination == _task.destination){
                return true;
            }
        }
        return false;
    }

    private void routeMessage(Message _message){
        if(adHocNetwork){
            if(_message.relayCheck()){
                send(_message);
            }
        }
    }

    private void send(Message _message){
        medium.broadcast(transform.position, _message);
    }

    public void OnDrawGizmos(){
        switch (state){
            case State.idle: Gizmos.color = freeColor; break;
            case State.driving: Gizmos.color = drivingColor; break;
            case State.waiting: Gizmos.color = waitingColor; break;
            default: Gizmos.color = freeColor; break;
        }
        if(isConnected){
            Gizmos.DrawSphere(transform.position, 1.0f);
        }else{
            Gizmos.DrawCube(transform.position, new Vector3(1.6f, 1.6f, 1.6f));
        }

        if(assistanceRouteDisplayTimeout > 0){
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(requestedAssistanceRoute[0], 2.0f);
            Gizmos.color = Color.green;
            for(int i = 1; i < requestedAssistanceRoute.Count - 1; i++){
                Gizmos.DrawLine(requestedAssistanceRoute[i - 1], requestedAssistanceRoute[i]);
                Gizmos.DrawSphere(requestedAssistanceRoute[i], 2.0f);
            }
            Gizmos.DrawLine(requestedAssistanceRoute[requestedAssistanceRoute.Count - 2], requestedAssistanceRoute[requestedAssistanceRoute.Count - 1]);
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(requestedAssistanceRoute[requestedAssistanceRoute.Count - 1], 2.0f);
            assistanceRouteDisplayTimeout -= Time.deltaTime;
        }
    }
}
