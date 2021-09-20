using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Message{
    public int recipient;
    public Type type;
    private int relayCounter;

    public enum Type{
        standard,
        task,
        status,
        acknowledge,
        knowledgeExchange,
        relayOffer,
        relayBid
    }

    public Message(Message.Type _type = Message.Type.standard, int _recipient = -1){
        type = _type;
        recipient = _recipient;
        relayCounter = 0;
    }

    public bool relayCheck(){
        return ++relayCounter < 3;
    }
}

public class TaskMessage : Message{
    public GraphNode destination;
    public float waitTime;

    public bool isRelayTask;

    public TaskMessage(int _recipient, GraphNode _destination, float _waitTime, bool _isRelayTask = false){
        type = Type.task;
        recipient = _recipient;
        destination = _destination;
        waitTime = _waitTime;
        isRelayTask = _isRelayTask;
    }
}

public class StatusMessage : Message{
    public MobileAgent.State state;
    public int sender;
    public int numberOfCompletedTasks;
    public Vector3 agvPosition;
    public bool isConnected;

    public StatusMessage(MobileAgent.State _state, int _sender, Vector3 _agvPosition,bool _isConnected = false, int _numberOfCompletedTasks = 0){
        type = Type.status;
        recipient = -1;
        state = _state;
        sender = _sender;
        agvPosition = _agvPosition;
        numberOfCompletedTasks = _numberOfCompletedTasks;
        isConnected = _isConnected;
    }
}

public class StatusAcknowledgeMessage : Message{
    public StatusAcknowledgeMessage(int _recipient){
        recipient = _recipient;
        type = Message.Type.acknowledge;
    }
}

public class KnowledgeExchangeMessage : Message{
    public knowledgeDataSet dataSet;

    public KnowledgeExchangeMessage(knowledgeDataSet _dataSet){
        type = Message.Type.knowledgeExchange;
        recipient = 0;
        dataSet = _dataSet;
    }
}

public class RelayOfferMessage : Message{
    public GraphNode relayPosition;
    public int sender;

    public RelayOfferMessage(int _sender, GraphNode _relayPosition){
        type = Type.relayOffer;
        relayPosition = _relayPosition;
        sender = _sender;
        recipient = 0;
    }
}

public class RelayBidMessage : Message{
    public float distanceToRelayPosition;
    public int sender;
    public GraphNode bidFor;
    public RelayBidMessage(int _sender, int _recipient, float _distance, GraphNode _bidFor){
        type = Message.Type.relayBid;
        sender = _sender;
        recipient = _recipient;
        distanceToRelayPosition = _distance;
        bidFor = _bidFor;
    }
}

public class Tranmission{
    public static bool check(Vector3 _source, Vector3 _destination){
        float maxDistance = 40.0f;

        bool inMaxDistance = Vector3.Distance(_source, _destination) <= maxDistance;
        bool notSame = _source != _destination;
        bool notObstructed = numberOfObstacles(_source, _destination) <= 2;

        return inMaxDistance && notSame && notObstructed;
    }

    private static int numberOfObstacles(Vector3 _source, Vector3 _destination){
        RaycastHit[] hits;
        hits = Physics.RaycastAll(_source, _destination - _source, (_destination-_source).magnitude);
        return hits.Length;
    }
}
