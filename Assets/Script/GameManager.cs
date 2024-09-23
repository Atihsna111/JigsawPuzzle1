using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] Transform _gameTransform; // to scale the puzzle
    [SerializeField] Transform[] _piecePrefab; //array of prefabs for different puzzles
    
    [Header ("UI Objects")]
    [SerializeField] GameObject Button; //play button
    [SerializeField] GameObject GameUI; // UI elements
    [SerializeField] GameObject OutOfTimeText; //timeout text
    [SerializeField] TMPro.TextMeshProUGUI timeText; //text to display remaining time
    [SerializeField] private Text _LevelCompletetext; // text to show level complete
    [SerializeField] private Text _Name; // text to show game name

    [Header("Audio")]
     [SerializeField] AudioSource audioSource;
     [SerializeField]  AudioClip BGM;
    
    List<Transform> pieces; // List to store puzzle pieces
    private int _emptyLocation; //Location of the empty pieces
    private int _size = 4; //size of the puzzle grid
    private bool _shuffling = false; // flag to indicate the puzzle shuffle
    private int _currentLevel = 0; // current level 
    private bool _playing = false; //flag for playing status
    float _remainingTime; // remaining time
    float _startingTime = 300f; //starting time for each level

    void Awake()
    {
        _Name.gameObject.SetActive (true);
        pieces = new List<Transform>();
        OutOfTimeText.SetActive(false);
        _LevelCompletetext.gameObject.SetActive(false);  
        GameUI.gameObject.SetActive (false);
       
    } 
    public void StartGame()
    {
        audioSource.clip = BGM;
        audioSource.Play();
        ResetGame();
        CreateGamePiece(0.01f);
        StartCoroutine(CreateGamePiece(0.01f)); 
        StartCoroutine(WaitShuffle(0.9f));
        
        _remainingTime = _startingTime;
        _playing = true;
        
        //Show/hide the UI elements
        Button.SetActive(false);
        OutOfTimeText.SetActive(false);
        GameUI.SetActive(true);
        _LevelCompletetext.gameObject.SetActive(false);   
        _Name.gameObject.SetActive (false);       
    }

    private void ResetGame()
    {
       ClearPieces();
       ResetPuzzle();
       _shuffling = false;
       _playing = false;
       _remainingTime = _startingTime;
       //StopAllCoroutines();// to stop any ongoing coroutine

    }
    private void ClearPieces()
    {
         //Clear existing piece if any
       foreach(Transform piece in pieces)
       {
        Destroy(piece.gameObject);

       }
       pieces.Clear();
    }

    // Create the game setup with szie  x size pieces.
    private IEnumerator CreateGamePiece(float gapThickness)
    {  
        //Select the correct prefab based on the current
       Transform piecePrefab = _piecePrefab[_currentLevel]; 
        //the width of each tile.
        float width = 1/(float)_size;
        for(int row = 0; row < _size; row++)
        {
          for(int col = 0; col < _size; col++)
            {
                Transform piece = Instantiate(piecePrefab, _gameTransform);
                pieces.Add(piece);
                //piece positions within -1 to +1
                piece.localPosition = new Vector3(-1 + (2 * width * col) + width, +1 -(2 * width * row) - width,0);
                piece.localScale = ((2 * width) - gapThickness) * Vector3.one;
                piece.name = $"{(row * _size) + col}";
                //We want an empty space in the bottom right.
                if((row == _size -1) && (col == _size -1))
                {
                    _emptyLocation = (_size * _size) -1;
                    piece.gameObject.SetActive(false);
                }
                else
                {
                    //to map UV coordinates appropriate, they are 0 -> 1
                    float gap = gapThickness / 2;
                    Mesh mesh = piece.GetComponent<MeshFilter>().mesh;
                    Vector2[] uv = new Vector2[4];
                    //UV coordination order: (0,1), (1,1), (0,0), (1,0)
                    uv[0] = new Vector2((width * col)+ gap, 1 - ((width * (row +1)) - gap));
                    uv[1] = new Vector2((width * (col +1)) - gap, 1 - ((width * (row +1)) - gap));
                    uv[2] = new Vector2((width * col)+ gap, 1 - ((width * row) + gap));
                    uv[3] = new Vector2((width * (col +1))- gap, 1 - ((width * row) + gap));
                    //to assign new UVs to the mesh.
                    mesh.uv = uv;                   
                }
            }
        }
        yield return null;      
    }
    //Function to level progression
    private IEnumerator NextLevel()
    {
        
        _currentLevel++;
        _size++;
        if(_currentLevel >= _piecePrefab.Length)
        {
            _currentLevel = 0; //Reset to the first prefab if we run
        }
        Debug.Log("Congrations! You have completed level" + (_currentLevel -1));
        yield return new WaitForSeconds(1f);
        ResetPuzzle();
        StartCoroutine(CreateGamePiece(0.01f));
        StartCoroutine(WaitShuffle(0.9f));
    }

    public void ResetPuzzle()
    {
        
        //create a new puzzle for the next level
        CreateGamePiece(0.01f); 
        ClearPieces();
        _LevelCompletetext.gameObject.SetActive(false);
        _remainingTime = _startingTime;
        // start shuffling for the next level
        StartCoroutine(WaitShuffle(0.9f)); 
    }
     //column Check is used to stop horizontal moves wrapping.
    private bool SwapIfValid(int i, int offset, int colCheck)
    {
        int _targetIndex = i + offset;
        if(i >=0 && i < pieces.Count && _targetIndex >=0 && _targetIndex < pieces.Count)
        {
            if(((i % _size) != colCheck) && (_targetIndex == _emptyLocation))
            {
                (pieces[i], pieces[i + offset]) = (pieces[i + offset], pieces[i]);
                // Swap their transforms.
                (pieces[i].localPosition, pieces[i + offset].localPosition) = (pieces[i + offset].localPosition, pieces[i].localPosition);
                // Update empty location.
                _emptyLocation = i;
                //Check for completion
                if(!_shuffling && CheckCompletion())
                {
                    _shuffling = true;
                    StartCoroutine(NextLevel());
                   StartCoroutine(WaitShuffle(0.2f));
                }
                return true;
            }
         }
        return false;
    }
    private bool CheckCompletion()
    {
        for(int i = 0; i < pieces.Count; i++)
        {
            if(pieces[i].name != $"{i}")
            {
                return false;
            }
        }
        _LevelCompletetext.gameObject.SetActive(true);
        return true;
    }
    private IEnumerator WaitShuffle(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        Shuffle();
        _shuffling = false;
    }
    private void Shuffle()
    {
        _shuffling = true;// Indicate that the game is currently shuffling
        int moveCount = 0;
        int last = 0;
        int _totalmoves = _size * _size* _size;
        while(moveCount < _totalmoves)
        {
            int position = Random.Range(0, _size*_size);
            //prevent undoing the last move
            if(position == last)
            {
                continue;
            }
            last = _emptyLocation;
            //Get Valid moves from the current postion
            //Attempt each valid move
            if(SwapIfValid(position, -_size, _size)){moveCount++;} 
            if(SwapIfValid(position, +_size, _size)){moveCount++;}  
            if(SwapIfValid(position, -1, 0)){moveCount++;}
            if(SwapIfValid(position, +1, _size -1))

            {
                moveCount++;
            }    
        }
    }
   
    // Update is called once per frame
    public void Update()
    {

        if (_playing == true)
        {
            //update time
            _remainingTime -= Time.deltaTime;
            if(_remainingTime <= 0)
            {
                _remainingTime = 0;
                GameOver(0);
            }
            int _minutes = (int)(_remainingTime / 60);
            int _seconds = (int)(_remainingTime % 60);
            timeText.text = $"{_minutes}:{_seconds:D2}";
            Debug.Log($"Remaining time: {_remainingTime}, Minutes: {_minutes}, Seconds: {_seconds}");
        }
        if(Input.GetMouseButtonDown(0))
        {
         RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
        if(hit)
        {
         for(int i = 0; i < pieces.Count; i++)
        {
         if(pieces[i] == hit.transform)
         {
            //Check valid direction to swap. If successful we don't swap that piece again
            //to size element up
            if(SwapIfValid(i, -_size, _size))
            {
                break;
            } 
            //to size element down
            if(SwapIfValid(i, +_size, _size))
            {
                break;
            }
            //to swipe left
            if(SwapIfValid(i, -1, 0))
            {
                break;
            }
            //to swipe right
            if(SwapIfValid(i, +1, _size -1))
            {
               break;
            }
            }
            }
            }
        }     
    }
    void GameOver(int type )
    {
        if(type ==0)
        {
            OutOfTimeText.gameObject.SetActive(true);    
            StopGame();
            ResetPuzzle();
            audioSource.Pause();
            
            
        }
        else{
            _LevelCompletetext.gameObject.SetActive(true);
            StopGame();
            audioSource.Pause();
        }
        
        _playing = false;
        Button.SetActive(true);

    }
    void StopGame()
    {
        _playing = false;
        StopAllCoroutines();

    }

    

}
