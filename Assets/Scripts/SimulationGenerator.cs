using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationGenerator : MonoBehaviour{

    public int seed = -1;
    public float factorySize = -1.0f;
    public float navGraphDenisty = -1.0f;
    public float mobileAgentDensity = 0.5f;
    public float APDensity = 0.75f;
    public bool manhattanAPs = false;
    public int numberOfAGVs;
    public int numberOfAPs;

    public bool generationDone;

    public Vector2 factoryBounds;
    public GameObject mobileAgentPrefab;
    public GameObject accessPointPrefab;
    public GameObject obstaclePrefab;

    private GameObject cameraRef;
    private GameObject ground;
    private Graph navGraph;
    private GameObject mobileAgentGroup;
    private GameObject APGroup;
    private GameObject obstacleGroup;
    private ResultLogger logger;

    public bool AGVsAreAdHoc = false;
    public bool AGVsAreAdaptive = false;

    void Awake(){
        cameraRef = GameObject.Find("Camera");
        ground = GameObject.Find("Ground");
        navGraph = GameObject.Find("NavGraph").GetComponent<Graph>();
        mobileAgentGroup = GameObject.Find("MobileAgentGroup");
        APGroup = GameObject.Find("APGroup");
        obstacleGroup = GameObject.Find("ObstacleGroup");
        logger = GameObject.Find("ResultLogger").GetComponent<ResultLogger>();
    }

    public void newConfiguration(int _seed = 0, float _factorySize = -1.0f, float _navGraphDenisty = -1.0f){
        generationDone = false;
        if(_seed == 0){
            if(seed == 0){
                seed = (int)(System.DateTime.Now.Ticks);
            }
        }else{
            seed = _seed;
        }
        Random.InitState(seed);

        factoryBounds = setupGround(_factorySize);
        resetCamera(factoryBounds.x > factoryBounds.y ? factoryBounds.x : factoryBounds.y);
        generateNavGraph(factoryBounds, _navGraphDenisty);
        numberOfAGVs = placeMobileAgents();
        numberOfAPs = placeAPs(numberOfAGVs, factoryBounds,manhattanAPs);
        StartCoroutine(placeObstacles(0));
    }

    public void reconfigure(float _disabledAPsPercentage){
        Debug.Log("Reconfiguring");
        int numberOfObstaclesInFactory = obstacleGroup.transform.childCount;
        int numberOfReconfiguredObstacles = (int)Mathf.Round(Random.Range(0.1f * numberOfObstaclesInFactory, 0.4f * numberOfObstaclesInFactory));
        deleteRandomObjectsInGroup(numberOfReconfiguredObstacles, obstacleGroup);
        reconfigureAPs(_disabledAPsPercentage);
        StartCoroutine(placeObstacles(numberOfReconfiguredObstacles,false));
    }

    private void deleteRandomObjectsInGroup(int _num, GameObject _group){
        List<int> randomIndices = new List<int>();
        for(int i = 0; i < _num; i++){
            randomIndices.Add(Random.Range(0, _group.transform.childCount));
        }
        randomIndices.Sort((a, b) => b.CompareTo(a));
        foreach(int index in randomIndices){
            GameObject deletionObstacle = _group.transform.GetChild(index).gameObject;
            Destroy(deletionObstacle);
        }
    }

    private void reconfigureAPs(float _disabledAPsPercentage){
        for(int i = 0; i < APGroup.transform.childCount; i++){
            APGroup.transform.GetChild(i).gameObject.GetComponent<AccessPoint>().working = Random.Range(0.0f, 1.0f) > _disabledAPsPercentage ? true : false;
        }
    }

    IEnumerator placeObstacles(int _num = 0, bool _log = true){
        int numberOfObstaclesToPlace = _num == 0 ? Random.Range(10,100) : _num;
        int staticObstacleNumber = numberOfObstaclesToPlace;
        int currentRetries = 0;
        while(numberOfObstaclesToPlace > 0){
            //Place a random obstacle
            Vector3 randomPlace = new Vector3(Random.Range(-factoryBounds.x/2.0f, factoryBounds.x/2.0f),0.0f, Random.Range(-factoryBounds.y/2.0f, factoryBounds.y/2.0f));
            Vector3 randomSize = new Vector3(Random.Range(1.0f, 10.0f), 1.0f, Random.Range(1.0f, 10.0f));
            GameObject newObstacle = Instantiate(obstaclePrefab, obstacleGroup.transform);
            newObstacle.transform.position = randomPlace;
            newObstacle.transform.localScale = randomSize;
            yield return null;

            //Check if it obstructs graph
            int numberOfCollisions = navGraph.checkForColissions();
            if(numberOfCollisions > 0){
                //remove if it does
                Destroy(newObstacle);
                currentRetries++;
            }else{
                //dont remove if it doesn't
                numberOfObstaclesToPlace--;
                currentRetries = 0;
            }
            yield return null;
            
            if(currentRetries > 50){
                this.StopAllCoroutines();
                GameObject.Find("SimulationController").GetComponent<SimulationController>().newSimulation();
            }
        }

        if(_log){
            logConfiguration(seed, factoryBounds, numberOfAGVs, numberOfAPs, staticObstacleNumber);
        }
        generationDone = true;

    }

    private void logConfiguration(int _seed, Vector2 _factorySize, int _numberOfAGVs, int _numberOfAPs, int _numberOfObstacles){
        string seedString = "seed;" + _seed.ToString();
        string factorySizestring = "size;" + _factorySize.x.ToString() + ";" + _factorySize.y.ToString();
        string agvString = "number of AGVs;" + _numberOfAGVs.ToString();
        string apString = "number of APs;" + _numberOfAPs.ToString();
        string obstacleString = "number of obstacles;" + _numberOfObstacles.ToString();
        string adhocString = "ad hoc network;" + (AGVsAreAdHoc ? "1" : "0");
        string adaptiveString = "adaptive network;" + (AGVsAreAdaptive ? "1" : "0");
        logger.log(seedString);
        logger.log(factorySizestring);
        logger.log(agvString);
        logger.log(apString);
        logger.log(obstacleString);
        logger.log(adhocString);
        logger.log(adaptiveString);
        logger.log("");
        logger.log("Logs:");
    }

    private int placeMobileAgents(){
        int placedMobileAgents = 0;
        List<GraphNode> parkingNodeList = navGraph.getNodesOfType(GraphNode.Type.task);
        foreach(GraphNode node in parkingNodeList){
            if(Random.Range(0.0f, 1.0f) > mobileAgentDensity){
                GameObject newAgent = Instantiate(mobileAgentPrefab, mobileAgentGroup.transform);
                newAgent.transform.position = node.position;
                MobileAgent newInstance = newAgent.GetComponent<MobileAgent>();
                newInstance.address = placedMobileAgents + 1;
                newInstance.adHocNetwork = AGVsAreAdHoc;
                newInstance.adaptive = AGVsAreAdaptive;
                placedMobileAgents++;
            }
        }
        return placedMobileAgents;
    }

    private int placeAPs(int _numberOfMobileAgents, Vector2 _factorySize, bool _asManhattan){
        int placedAPs = 0;
        if(!_asManhattan){
            int numberOfAPs = (int)(Mathf.Round(_numberOfMobileAgents * APDensity));
            while(numberOfAPs > 0){
                GameObject newAP = Instantiate(accessPointPrefab, APGroup.transform);
                newAP.transform.position = new Vector3(Random.Range(-_factorySize.x/2.0f, _factorySize.x/2.0f), 0.0f, Random.Range(-_factorySize.y/2.0f, _factorySize.y/2.0f));
                numberOfAPs--;
                placedAPs++;
            }
        }else{
            float biggerSide = _factorySize.x > _factorySize.y ? _factorySize.x : _factorySize.y;
            int APsPerSide = (int)(Mathf.Ceil(biggerSide/50.0f));
            List<Vector3> APPositions = generateManhattanGrid(_factorySize,APsPerSide);
            foreach(Vector3 APPosition in APPositions){
                GameObject newAP = Instantiate(accessPointPrefab, APGroup.transform);
                newAP.transform.position = APPosition;
                placedAPs++;
            }
        }
        return placedAPs;
    }

    private Vector2 setupGround(float _factorySize = -1.0f){
        factorySize = _factorySize > 0.0f ? _factorySize : Random.Range(10000.0f, 15000.0f); //(7000.0f, 150000.0f);
        float factorySideRelation = Random.Range(0,1) == 0 ? Random.Range(0.33f, 1.0f) : Random.Range(1.0f,3.0f);
        float factorySizeX = Mathf.Sqrt(factorySize * factorySideRelation);
        float factorySizeZ = factorySizeX / factorySideRelation;

        ground.transform.localScale = new Vector3(factorySizeX, factorySizeZ, 1.0f);

        return new Vector2(factorySizeX, factorySizeZ);
    }

    private void resetCamera(float _height){
        Vector3 cameraPosition = cameraRef.transform.position;
        cameraRef.transform.position = new Vector3(cameraPosition.x, _height, cameraPosition.z);
    }

    private void generateNavGraph(Vector2 _factorySize, float _navGraphDenisty = -1.0f){
        // Basic setup
        navGraphDenisty = _navGraphDenisty > 0.0f ? _navGraphDenisty : Random.Range(7.0f, 20.0f);
        int nodesPerSide = (int)Mathf.Round(Mathf.Sqrt((_factorySize.x*_factorySize.y)/(Mathf.Pow(navGraphDenisty,2.0f))));
        
        generateManhattanGraph(_factorySize, nodesPerSide);
        iregulateGraph(Random.Range(0.1f, 0.3f));
        addAGVStations(1.0f);
        addParkingNodes();
    }

    private void addParkingNodes(){
        float minDistance = 5.0f;
        List<GraphNode> taskNodes = navGraph.getNodesOfType(GraphNode.Type.task);
        foreach(GraphNode node in taskNodes){
            bool noCloseNode = true;
            foreach(GraphNode otherNode in taskNodes){
                if(Vector3.Distance(node.position, otherNode.position) < minDistance && otherNode.nodeType == GraphNode.Type.task && node != otherNode){
                    noCloseNode = false;
                    break;
                }
            }
            if(!noCloseNode){
                node.nodeType = GraphNode.Type.parking;
            }
        }
    }

    private void addAGVStations(float _prevelance){
        int numberOfConnections = navGraph.graphConnections.Count;
        int numberOfConnectionsToProcess = (int)Mathf.Round(numberOfConnections * _prevelance);
        int numberOfPocessedConnections = 0;

        while(numberOfPocessedConnections < numberOfConnectionsToProcess){
            bool selectedAConnection = false;
            int selectionIndex = 0;
            while(!selectedAConnection){
                selectionIndex = Random.Range(0, navGraph.graphConnections.Count);
                if(navGraph.getConnectionsLength(selectionIndex) > 5.0f){
                    selectedAConnection = true;
                }
            }

            addAGVStation(selectionIndex);
            numberOfPocessedConnections++;
        }
    }

    private void addAGVStation(int _connectionIndex){
        GraphConnection modifiedConnection = navGraph.graphConnections[_connectionIndex];
        bool isNorthSouth = modifiedConnection.startNode.position.x == modifiedConnection.endNode.position.x;

        //add center node
        int newNode = navGraph.halfConnection(_connectionIndex);
        Vector3 newNodePosition = navGraph.graphNodes[newNode].position;
        //figure out position of station nodes
        Vector3 leftPosition = Vector3.up;
        Vector3 rightPosition = Vector3.down;
        if(isNorthSouth){
            leftPosition = new Vector3(newNodePosition.x + 2.0f, newNodePosition.y, newNodePosition.z);
            rightPosition = new Vector3(newNodePosition.x - 2.0f, newNodePosition.y, newNodePosition.z);
        }else{
            leftPosition = new Vector3(newNodePosition.x, newNodePosition.y, newNodePosition.z + 2.0f);
            rightPosition = new Vector3(newNodePosition.x, newNodePosition.y, newNodePosition.z - 2.0f);
        }
        //spawn and connect nodes
        if(positionOverGround(leftPosition)){
            int leftNode = navGraph.addNode(leftPosition, GraphNode.Type.task);
            navGraph.connectNodes(newNode, leftNode, false);
        }
        if(positionOverGround(rightPosition)){
            int rightNode = navGraph.addNode(rightPosition, GraphNode.Type.task);
            navGraph.connectNodes(newNode, rightNode, false);
        }
    }

    private bool positionOverGround(Vector3 _position){
        bool fitsInX = _position.x > -(ground.transform.localScale.x / 2.0f) && _position.x < (ground.transform.localScale.x / 2.0f);
        bool fitsInY = _position.z > -(ground.transform.localScale.y / 2.0f) && _position.z < (ground.transform.localScale.y / 2.0f);
        return fitsInX && fitsInY;
    }

    private void iregulateGraph(float _holePercentage){
        int numberOfNodes = navGraph.getNumberOfNodes();
        int numberOfHoles = (int)Mathf.Round(numberOfNodes * _holePercentage);

        for(int i=0; i<numberOfHoles; i++){
            int deleteIndex = Random.Range(0, navGraph.getNumberOfNodes() - 1 - i);
            if(deleteIndex < navGraph.getNumberOfNodes()){
                navGraph.deleteNode(deleteIndex);
            }
        }
    }

    private void generateManhattanGraph(Vector2 _factorySize, int _nodesPerSide){
        List<Vector3> manhattanCoords = generateManhattanGrid(_factorySize, _nodesPerSide);
        int numberOfNodes = manhattanCoords.Count;
        foreach (Vector3 coordinates in manhattanCoords){
            navGraph.addNode(coordinates);
        }

        // Connect initial nodes
        for(int i=0; i < numberOfNodes - 1; i++){
            if((i + 1) % _nodesPerSide != 0){
                navGraph.connectNodes(i, i + 1);
            }
            if(i < (numberOfNodes - 1 - _nodesPerSide)){
                navGraph.connectNodes(i, i + _nodesPerSide);
            }
        }
    }

    private List<Vector3> generateManhattanGrid(Vector2 _factorySize, int _nodesPerSide){

        List<Vector3> coords = new List<Vector3>();

        float deltaX = _factorySize.x / (_nodesPerSide - 1);
        float deltaZ = _factorySize.y / (_nodesPerSide - 1);
        float offsetX = _factorySize.x / 2.0f;
        float offsetZ = _factorySize.y / 2.0f;

        for(float x = -offsetX; x <= offsetX + 1.0f; x += deltaX){
            for(float z = -offsetZ; z <= offsetZ + 1.0f; z += deltaZ){
                coords.Add(new Vector3(x, 0.0f, z));
            }
        }

        return coords;
    }

}
