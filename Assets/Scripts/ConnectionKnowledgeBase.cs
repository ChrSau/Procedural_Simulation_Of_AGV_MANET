using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class knowledgeDataSet{
    public Vector3 position;
    public ulong visitedCounter;
    public ulong connectedCounter;

    public knowledgeDataSet(Vector3 _position){
        position = _position;
        visitedCounter = 0;
        connectedCounter = 0;
    }

    public void register(bool _connected){
        visitedCounter = visitedCounter + 1;
        if(_connected){connectedCounter = connectedCounter + 1;}
    }

    public void add(knowledgeDataSet _set){
        visitedCounter += _set.visitedCounter;
        connectedCounter += _set.connectedCounter;
    }

    public float getConnectionProbability(){
        if(visitedCounter > 0){
            float prob = ((float)(connectedCounter))/((float)(visitedCounter));
            return prob;
        }else{
            return -1.0f;
        }
    }
}

public class ConnectionKnowledgeBase : MonoBehaviour{

    private Graph navGraph;

    private List<knowledgeDataSet> dataBase = new List<knowledgeDataSet>();

    private float initializationTimeOut = 2.0f;
    private bool isInitilized = false;

    public int edits = 0;
    public int expansions = 0;

    void Update(){
        initializationTimeOut -= Time.deltaTime;
        if(initializationTimeOut < 0.0f && !isInitilized){
            initialize();
        }
    }

    private void initialize(){
        if(isInitilized){
            return;
        }
        navGraph = GameObject.Find("NavGraph").GetComponent<Graph>();
        dataBase.Clear();
        List<GraphNode> nodesToAdd = new List<GraphNode>();
        nodesToAdd.AddRange(navGraph.getNodesOfType(GraphNode.Type.task));
        nodesToAdd.AddRange(navGraph.getNodesOfType(GraphNode.Type.parking));
        foreach(GraphNode node in nodesToAdd){
            dataBase.Add(new knowledgeDataSet(node.position));
        }


        isInitilized = true;
    }

    public void registerMoment(Vector3 _position, bool _connected){
        if(!isInitilized){
            return;
        }

        int relevantIndex = getClosestDataSetIndex(_position);
        dataBase[relevantIndex].register(_connected);
        edits++;
    }

    public float getConnectionProbability(Vector3 _position){
        if(!isInitilized){
            return -1.0f;
        }

        return dataBase[getClosestDataSetIndex(_position)].getConnectionProbability();
    }

    public KnowledgeExchangeMessage getRandomExchangeMessage(){
        if(!isInitilized){
            return null;
        }

        KnowledgeExchangeMessage message = new KnowledgeExchangeMessage(dataBase[Random.Range(0, dataBase.Count)]);
        return message;
    }

    public void incorporate(KnowledgeExchangeMessage _message){
        if(!isInitilized){
            return;
        }

        dataBase[getClosestDataSetIndex(_message.dataSet.position)].add(_message.dataSet);
        expansions++;
    }

    private int getClosestDataSetIndex(Vector3 _position){
        int closestKnownDataSetIndex = 0;
        float closestDistance = Mathf.Infinity;
        for(int i = 1; i < dataBase.Count; i++){
            float distance = Vector3.Distance(dataBase[i].position, _position);
            if(distance < closestDistance){
                closestDistance = distance;
                closestKnownDataSetIndex = i;
            }
        }
        return closestKnownDataSetIndex;
    }
}
