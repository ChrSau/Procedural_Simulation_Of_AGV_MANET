using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphNode{
    public Vector3 position;
    public Type nodeType;
    
    public enum Type{
        standard,
        task,
        parking
    }

    public GraphNode(Vector3 _position, Type _nodeType = Type.standard){
        position = _position;
        nodeType = _nodeType;
    }
}

public class GraphConnection{
    public GraphNode startNode;
    public GraphNode endNode;
    public bool geometric;
    public bool oneWay;
    public float weigth;

    public GraphConnection(GraphNode _startNode, GraphNode _endNode, bool _geometric = true, bool _oneWay = false, float _weight = 0.0f){
        startNode = _startNode;
        endNode = _endNode;
        geometric = _geometric;
        oneWay = _oneWay;
        weigth = _weight;
    }

    public float length(){
        return Vector3.Distance(endNode.position, startNode.position);
    }

    public float getWeight(){
        return geometric ? this.length() : weigth;
    }

    public void setWeight(float _weight){
        weigth = _weight;
    }

    public int checkForCollisions(){
        int numberOfDetectedHits = 0;
        RaycastHit[] hits;
        hits = Physics.RaycastAll(startNode.position, endNode.position - startNode.position, (endNode.position - startNode.position).magnitude);
        numberOfDetectedHits += hits.Length;
        hits = Physics.RaycastAll(endNode.position, endNode.position - startNode.position, (endNode.position - startNode.position).magnitude);

        numberOfDetectedHits += hits.Length;
        return numberOfDetectedHits;
    }
}

public class Graph : MonoBehaviour{

    public List<GraphNode> graphNodes = new List<GraphNode>();
    public List<GraphConnection> graphConnections = new List<GraphConnection>();

    public Color[] gizmoColors = {Color.black, Color.red, Color.blue, Color.green, Color.white};
    public bool hide = false;
    private int numberOfNodes = 0;

    public void toggleVisibility(){
        hide = !hide;
    }

    public int addNode(Vector3 _positon, GraphNode.Type _nodeType = GraphNode.Type.standard){
        graphNodes.Add(new GraphNode(_positon, _nodeType));
        return graphNodes.Count-1;
    }

    public int getNumberOfNodes(){
        return graphNodes.Count;
    }

    public List<GraphNode> getNodesOfType(GraphNode.Type _nodeType){
        List<GraphNode> nodeList = new List<GraphNode>();
        foreach (GraphNode node in graphNodes){
            if(node.nodeType == _nodeType){
                nodeList.Add(node);
            }
        }
        return nodeList;
    }

    public int connectNodes(int _nodeA, int _nodeB, bool _oneWay=false){
        GraphNode nodeA = graphNodes[_nodeA];
        GraphNode nodeB = graphNodes[_nodeB];
        if(_nodeA != _nodeB && !connectionExists(nodeA,nodeB)){
            graphConnections.Add(new GraphConnection(nodeA, nodeB, true, _oneWay));
            return graphConnections.Count-1;
        }else{
            return -1;
        }
    }

    public int connectNodes(int _nodeA, int _nodeB, float _weight, bool _oneWay=false){
        GraphNode nodeA = graphNodes[_nodeA];
        GraphNode nodeB = graphNodes[_nodeB];
        if(_nodeA != _nodeB && !connectionExists(nodeA,nodeB)){
            graphConnections.Add(new GraphConnection(nodeA, nodeB, false, _oneWay, _weight));
            return graphConnections.Count-1;
        }else{
            return -1;
        }
    }

    public void deleteNode(int _nodeIndex){
        numberOfNodes = graphNodes.Count;
        if(_nodeIndex < 0 || _nodeIndex > numberOfNodes-1){
            Debug.Log("Wanted to delete invalid node.");
            return;
        }

        GraphNode nodeToDelete = graphNodes[_nodeIndex];

        //Delete connections from or to the nodes
        if(_nodeIndex > 0 && _nodeIndex <= numberOfNodes){
            List<GraphConnection> connectionsToRemove = new List<GraphConnection>();
            foreach (GraphConnection connection in graphConnections){
                if(connection.startNode == nodeToDelete || connection.endNode == nodeToDelete){
                    connectionsToRemove.Add(connection);
                }
            }
            foreach(GraphConnection connection in connectionsToRemove){
                graphConnections.Remove(connection);
            }

            graphNodes.RemoveAt(_nodeIndex);
            numberOfNodes--;
        }
    }

    public float getConnectionsLength(int _connectionIndex){
        Vector3 startPoint = graphConnections[_connectionIndex].startNode.position;
        Vector3 endPoint = graphConnections[_connectionIndex].endNode.position;

        return Vector3.Distance(startPoint, endPoint);
    }

    public int halfConnection(int _connectionIndex){
        GraphConnection modifiedConnection = graphConnections[_connectionIndex];
        Vector3 oldStartPosition = modifiedConnection.startNode.position;
        int oldStartIndex = graphNodes.IndexOf(modifiedConnection.startNode);
        Vector3 oldEndPosition = modifiedConnection.endNode.position;
        int oldEndIndex = graphNodes.IndexOf(modifiedConnection.endNode);
        
        Vector3 centerNodePosition = new Vector3((oldStartPosition.x + oldEndPosition.x)/2.0f,(oldStartPosition.y + oldEndPosition.y)/2.0f,(oldStartPosition.z + oldEndPosition.z)/2.0f);

        //remove the old connection
        graphConnections.Remove(modifiedConnection);
        //add the new node
        int newNode = addNode(centerNodePosition);
        //connect new node up
        connectNodes(oldStartIndex, newNode, modifiedConnection.oneWay);
        connectNodes(newNode, oldEndIndex, modifiedConnection.oneWay);
        return newNode;
    }

    public GraphNode getClosestNode(Vector3 _position){
        float closestDistance = Mathf.Infinity;
        GraphNode closestNode = new GraphNode(Vector3.zero);

        foreach(GraphNode node in graphNodes){
            float distance = Vector3.Distance(_position, node.position);
            if(distance < closestDistance){
                closestDistance = distance;
                closestNode = node;
            }
        }

        return closestNode;
    }

    public int getClosestNodeIndex(Vector2 _position){
        return graphNodes.IndexOf(getClosestNode(_position));
    }

    public List<GraphNode> getRoute(GraphNode _start, GraphNode _destination){
        int startIndex = graphNodes.IndexOf(_start);
        int endIndex = graphNodes.IndexOf(_destination);

        List<List<float>> distMat = getDistMat();

        List<int> indexRoute = shortestPath(startIndex, endIndex, Dijkstra(distMat, startIndex));

        List<GraphNode> route = new List<GraphNode>();

        foreach(int index in indexRoute){
            route.Add(graphNodes[index]);
        }

        return route;
    }

    private List<int> shortestPath(int start, int target, List<int> vorganger){
        List<int> revPath = new List<int>();
        revPath.Add(target);
        int u = target;
        while (vorganger[u] != -1){
            u = vorganger[u];
            revPath.Add(u);
        }
        revPath.Add(start);

        List<int> path = new List<int>();
        for (int i = revPath.Count - 1; i >= 0; i--){
            path.Add(revPath[i]);
        }

        return path;
    }

    public List<int> Dijkstra(List<List<float>> conMat, int start){
        List<float> abstand = new List<float>();
        List<int> vorganger = new List<int>();
        List<int> Q = new List<int>();
        for (int i = 0; i < conMat.Count; i++){
            abstand.Add(Mathf.Infinity);
            vorganger.Add(-1);
            Q.Add(i);
        }
        abstand[start] = 0;

        while (Q.Count > 0){
            int u = smallestDistInQ(Q, abstand);
            Q.Remove(u);
            List<int> uN = neighbourOfU(conMat, u);
            for (int i = 0; i < uN.Count; i++){
                int v = uN[i];
                if (Q.Contains(v)){
                    float altDist = abstand[u] + conMat[u][v];
                    if (altDist < abstand[v]){
                        abstand[v] = altDist;
                        vorganger[v] = u;
                    }
                }
            }
        }

        return vorganger;
    }

    public int smallestDistInQ(List<int> Q, List<float> abstand){
        //Debug.Log("MM - smallestDistInQ");
        int smallest = Q[0];
        float smallestDist = abstand[smallest];
        for (int i = 1; i < Q.Count; i++){
            float dist = abstand[Q[i]];
            if (dist < smallestDist){
                smallest = Q[i];
                smallestDist = dist;
            }
        }
        return smallest;
    }

    public List<int> neighbourOfU(List<List<float>> conMat, int U){
        //Debug.Log("MM - neighbourOfU");
        List<int> neighbours = new List<int>();
        for (int i = 0; i < conMat[U].Count; i++){
            if (conMat[U][i] < Mathf.Infinity){
                neighbours.Add(i);
            }
        }
        return neighbours;
    }

    private List<List<float>> getDistMat(){
        List<List<float>> matrix = initConMat();

        foreach(GraphConnection connection in graphConnections){
            int startIndex = graphNodes.IndexOf(connection.startNode);
            int endIndex = graphNodes.IndexOf(connection.endNode);
            float distance = connection.length();

            matrix[startIndex][endIndex] = distance;
            if(!connection.oneWay){
                matrix[endIndex][startIndex] = distance;
            }
        }

        return matrix;
    }

    private List<List<float>> initConMat(){
        List<List<float>> matrix = new List<List<float>>();

        for(int i=0;i<graphNodes.Count;i++){
            matrix.Add(new List<float>());
            for(int j=0;j<graphNodes.Count;j++){
                if(i == j){
                    matrix[i].Add(0.0f);
                }else{
                    matrix[i].Add(Mathf.Infinity);
                }
            }
        }

        return matrix;
    }

    public bool connectionExists(GraphNode _nodeA, GraphNode _nodeB){
        foreach(GraphConnection connection in graphConnections){
            if(connection.oneWay){
                if(connection.startNode == _nodeA && connection.endNode == _nodeB){
                    return true;
                }
            }else{
                if((connection.startNode == _nodeA && connection.endNode == _nodeB) || (connection.startNode == _nodeB && connection.endNode == _nodeA)){
                    return true;
                }
            }
        }
        return false;
    }

    public int getConnectionIndex(GraphNode _start, GraphNode _end){
        int index = -1;
        for(int i = 0; i < graphConnections.Count; i++){
            if(graphConnections[i].startNode == _start && graphConnections[i].endNode == _end){
                index = i;
                break;
            }
        }
        return index;
    }

    public int checkForColissions(){
        int numberOfCollisionsOnGraph = 0;

        foreach(GraphConnection connection in graphConnections){
            numberOfCollisionsOnGraph += connection.checkForCollisions();
        }
        //Debug.Log("Number of collisions: " + numberOfCollisionsOnGraph.ToString());
        return numberOfCollisionsOnGraph;
    }

    public void OnDrawGizmos(){
        if(!hide){
            foreach(GraphNode node in graphNodes){
                switch (node.nodeType){
                    case GraphNode.Type.standard: Gizmos.color = gizmoColors[0]; break;
                    case GraphNode.Type.task: Gizmos.color = gizmoColors[1]; break;
                    case GraphNode.Type.parking: Gizmos.color = gizmoColors[2]; break;
                    default: Gizmos.color = gizmoColors[0]; break;
                }
                Gizmos.DrawSphere(node.position, 0.5f);
            }
            foreach(GraphConnection connection in graphConnections){
                Gizmos.color = connection.oneWay ? gizmoColors[0] : gizmoColors[1];
                Gizmos.DrawLine(connection.startNode.position, connection.endNode.position);
            }
        }
    }

    public void clear(){
        graphNodes.Clear();
        graphConnections.Clear();
    }

    public void Update(){
        numberOfNodes = graphNodes.Count;
    }

}
