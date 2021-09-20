using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class RelayAssitance : MonoBehaviour{

    private List<Vector3> APPositions = new List<Vector3>();
    private List<Vector3> relayNodePositions = new List<Vector3>();

    private Graph navGraph;

    void Start(){
        updateAPPositions();

        navGraph = GameObject.Find("NavGraph").GetComponent<Graph>();
        List<GraphNode> parkingNodes = navGraph.getNodesOfType(GraphNode.Type.parking);
        foreach(GraphNode node in parkingNodes){
            relayNodePositions.Add(node.position);
        }
    }

    private void updateAPPositions(){
        Transform APGroup = GameObject.Find("APGroup").transform;
        for(int i = 0; i < APGroup.childCount; i++){
            if(APGroup.GetChild(i).GetComponent<AccessPoint>().working){
                APPositions.Add(APGroup.GetChild(i).position);
            }
        }
    }

    public List<GraphNode> getAssistanceRoute(GraphNode _destination){
        updateAPPositions();
        Vector3 closestAPPosition = getClosestNode(APPositions, _destination.position);
        List<Vector3> route = searchRoute(relayNodePositions, closestAPPosition, _destination.position, 25.0f, 0);
        List<GraphNode> routeNodes = new List<GraphNode>();
        foreach(Vector3 nodePosition in route){
            routeNodes.Add(navGraph.getClosestNode(nodePosition));
        }
        return routeNodes;
    }

    private List<Vector3> searchRoute(List<Vector3> _V, Vector3 _s, Vector3 _t, float _dmax, int _safety){
        //Debug.Log("Searching for route");
        float maxSearchFactor = 0.5f;

        if(Vector3.Distance(_s, _t) < _dmax){
            return new List<Vector3>();
        }

        //Minimum required number of nodes
        int minReq = (int)Mathf.Ceil(Vector3.Distance(_s, _t) / _dmax);

        //Distance of all nodes to the connection
        List<float> D = distanceToConnection(_V, _s, _t);

        //Sort nodes by their distance to the connection
        var sorted = D
            .Select((x, i) => new KeyValuePair<float, int>(x, i))
            .OrderBy(x => x.Key)
            .ToList();
        D = sorted.Select(x => x.Key).ToList();
        List<int> I = sorted.Select(x => x.Value).ToList();
        //sort nodes by corresponding distances
        List<Vector3> Vsort = new List<Vector3>();
        for(int i = 0; i < I.Count; i++){
            Vsort.Add(_V[I[i]]);
        }

        //Route is first entries of sorted nodes
        List<Vector3> R = new List<Vector3>();
        for(int i = 0; i < minReq + _safety; i++){
            R.Add(Vsort[i]);
        }

        //sort R by the distance to _s
        List<Vector3> rUnsorted = new List<Vector3>(R);
        R.Clear();
        for(int i = 0; i < rUnsorted.Count; i++){
            Vector3 closestNode = getClosestNode(rUnsorted, _s);
            rUnsorted.Remove(closestNode);
            R.Add(closestNode);
        }

        if(checkRoute(_s, R, _t, _dmax)){
            List<Vector3> CR = new List<Vector3>();
            CR.Add(_s);
            CR.AddRange(R);
            CR.Add(_t);
            CR = optimizeRoute(CR, _dmax);
            R = CR.GetRange(1, CR.Count - 2);
            return R;
        }else{
            //Debug.Log("Expand");
            if(_safety + minReq < _V.Count * maxSearchFactor){
                R = searchRoute(_V, _s, _t, _dmax, _safety + 1);
                return R;
            }else{
                List<Vector3> CR = new List<Vector3>();
                CR.Add(_s);
                CR.AddRange(R);
                CR.Add(_t);
                CR = optimizeRoute(CR, _dmax);
                R = CR.GetRange(1, CR.Count - 2);
                if(checkRoute(_s, R, _t, _dmax)){
                    //Debug.Log("Success in desperate opt");
                    return R;
                }else{
                    //Debug.Log("Error");
                    return new List<Vector3>();
                }
            }
        }

    }

    private List<Vector3> optimizeRoute(List<Vector3> _CR, float _dmax){
        for(int i = 1; i < _CR.Count - 1; i++){
            Vector3 cb = _CR[i-1];
            Vector3 ce = _CR[i+1];
            Vector3 c = _CR[i];
            if(Vector3.Distance(cb, ce) < _dmax){
                _CR.Remove(c);
                //Debug.Log("Optimized");
                _CR = optimizeRoute(_CR, _dmax);
                break;
            }
        }
        return _CR;
    }

    private bool checkRoute(Vector3 _s, List<Vector3> _R, Vector3 _t, float _dmax){
        if(Vector3.Distance(_s, _t) < _dmax){
            return true;
        }else{
            if(_R.Count < 1){
                return false;
            }
            bool startGood = Vector3.Distance(_s, _R[0]) < _dmax;
            bool routeGood = true;
            for(int i = 0; i < _R.Count - 2; i++){
                bool partGood = Vector3.Distance(_R[i], _R[i+1]) < _dmax;
                routeGood = routeGood && partGood;
            }
            bool endGood = Vector3.Distance(_R[_R.Count-1], _t) < _dmax;
            return startGood && routeGood && endGood;
        }
    }

    private Vector3 getClosestNode(List<Vector3> _nodes, Vector3 _targetNode){
        float smallestDistance = Mathf.Infinity;
        Vector3 closestNode = _nodes[0];
        foreach(Vector3 node in _nodes){
            float currentDistance = Vector3.Distance(node, _targetNode);
            if(currentDistance < smallestDistance){
                smallestDistance = currentDistance;
                closestNode = node;
            }
        }
        return closestNode;
    }

    private List<float> distanceToConnection(List<Vector3> _nodes, Vector3 _startNode, Vector3 _targetNode){
        List<float> distances = new List<float>();

        foreach(Vector3 node in _nodes){
            if(node != _startNode && node != _targetNode){
                distances.Add(Vector3.Distance(_startNode, node) + Vector3.Distance(node, _targetNode) - Vector3.Distance(_startNode, _targetNode));
            }else{
                distances.Add(Mathf.Infinity);
            }
        }

        return distances;
    }

}
