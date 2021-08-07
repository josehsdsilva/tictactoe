using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CartesianRobotController : MonoBehaviour
{
    public GameController gameController;
    public GameObject arm, top, balls, ballDropPoint;

    int spaceX, spaceY;
    float x = 1, z = 1, movX, movZ;
    Vector3 previousArmPosition, previousTopPosition;

    // Animation Control
    [Range(0.1f, 1f)]
    [SerializeField] float defaultAnimDuration = 0.1f;

    [HideInInspector] public int animationStatus = 0;
    float animationElapsedTime = 0.0f;
    float animationProgress = 0.0f;
    float animationDuration = 0.0f;
    int movementType;
    // 0 -  
    // 1 -  Get Ball to Play
    // 2 -  Move to Drop on board space
    // 3 -  Get Ball to reset
    // 4 -  Drop Ball to reset
    float scaleFactor = 5.4f;

    // Balls Information
    Vector3[,] ballsStartPosition;
    int[] ballUsed;
    int selectedBall;

    // Initialization
    void Awake()
    {
        ballUsed = new int[2];
        x = transform.position.x;
        z = transform.position.z;
        ballsStartPosition = new Vector3[2, 5];
        for (int player = 0; player < 2; player++)
        {
            for (int ball = 0; ball < 5; ball++)
            {
                ballsStartPosition[player, ball] = balls.transform.GetChild(player * 5 + ball).transform.position;
            }
        }
    }

    // Orders
    public void GetCircle()
    {
        Debug.Log(Global.instance.turn-1);
        Debug.Log(ballUsed[Global.instance.turn-1]);
        Debug.Log(ballsStartPosition[Global.instance.turn-1, ballUsed[Global.instance.turn-1]]);
        movementType = 1;
        movX = ballsStartPosition[Global.instance.turn-1, ballUsed[Global.instance.turn-1]].x - x;
        movZ = ballsStartPosition[Global.instance.turn-1, ballUsed[Global.instance.turn-1]].z - z;
        MoveZ();
    }

    public void Move(int _x, int _y, Vector3 pos)
    {
        movementType = 2;
        spaceX = _x;
        spaceY = _y;
        movX = pos.x - x;
        movZ = pos.z - z;
        MoveZ();
    }
        
    public void ResetBalls()
    {
        animationStatus = 0;

        for (int player = 0; player < 2; player++)
        {
            for (int ball = 0; ball < 5; ball++)
            {
                if(HasSamePosition(ballsStartPosition[player, ball], balls.transform.GetChild(player * 5 + ball).transform.position) == false)
                {
                    movementType = 3;
                    selectedBall = player * 5 + ball;
                    movX = balls.transform.GetChild(player * 5 + ball).transform.position.x - x;
                    movZ = balls.transform.GetChild(player * 5 + ball).transform.position.z - z;
                    MoveZ();
                    return;
                }
            }
        }

        // Game Reseted
        Debug.Log("Reset");
        gameController.SetOnGameSetup();
    }

    // Helpers
    bool HasSamePosition(Vector3 startPos, Vector3 currentPos)
    {
        if(startPos.x == currentPos.x && startPos.z == currentPos.z) return true;
        return false;
    }

    public void ResetBallUsed()
    {
        ballUsed[0] = 0;
        ballUsed[1] = 0;
    }

    // Arm Movement
    void MoveZ()
    {
        previousTopPosition = top.transform.position;
        animationStatus = 2;
        animationElapsedTime = 0;
        animationProgress = 0;
        animationDuration = defaultAnimDuration * Mathf.Abs(movZ);
    }

    void MoveX()
    {
        previousArmPosition = arm.transform.position;
        animationStatus = 3;
        animationElapsedTime = 0;
        animationProgress = 0;
        animationDuration = defaultAnimDuration * Mathf.Abs(movX);
    }

    void ScaleUpArm()
    {
        animationStatus = 4;
        animationElapsedTime = 0;
        animationProgress = 0;
        animationDuration = defaultAnimDuration;
    }

    void ScaleDownArm()
    {
        animationStatus = 5;
        animationElapsedTime = 0;
        animationProgress = 0;
        animationDuration = defaultAnimDuration;
    }

    void CatchBall()
    {
        balls.transform.GetChild(selectedBall).gameObject.SetActive(false);
        ScaleDownArm();
    }

    void DropBall()
    {
        balls.transform.GetChild(selectedBall).transform.position = ballDropPoint.transform.position;
        balls.transform.GetChild(selectedBall).gameObject.SetActive(true);
        ballUsed[Global.instance.turn-1]++;
        ScaleDownArm();
    }

    void PlaceOnDefaultPosition()
    {
        int player = selectedBall / 5;
        int ball = selectedBall;
        if(selectedBall >= 5) ball = selectedBall - 5;
        
        movementType = 4;
        movX = ballsStartPosition[player, ball].x - x;
        movZ = ballsStartPosition[player, ball].z - z;
        MoveZ();
    }


    // Update
    void FixedUpdate()
    {
        if(animationStatus != 0)
        {
            animationElapsedTime += Time.deltaTime;
            animationProgress = animationElapsedTime / animationDuration;
            if(animationProgress >= 1)
            {
                animationProgress = 1;
                if(animationStatus == 2) // MoveZ
                {
                    top.transform.position = previousTopPosition + new Vector3(0, 0, movZ);
                    z += movZ;
                    MoveX();
                }
                else if(animationStatus == 3) // MoveX
                {
                    arm.transform.position = previousArmPosition + new Vector3(movX, 0, 0);
                    x += movX;
                    ScaleUpArm();
                }
                else if(animationStatus == 4) // ScaleUpArm
                {
                    if(movementType == 1)
                    {
                        selectedBall = (Global.instance.turn-1) * 5 + ballUsed[Global.instance.turn-1];
                        CatchBall();
                    }
                    else if(movementType == 2 || movementType == 4)
                    {
                        DropBall();
                    }
                    else if(movementType == 3)
                    {
                        CatchBall();
                    }
                }
                else if(animationStatus == 5) // ScaleDownArm
                {
                    if(movementType == 1)
                    {
                        animationStatus = 0;
                        gameController.SetOnIdle();
                    }
                    else if(movementType == 2)
                    {
                        Global.instance.EndOfTurn(spaceX, spaceY);
                        if(gameController.IsWin() == false)
                        {
                            gameController.SetOnGameSetup();
                        }
                        else
                        {
                            animationStatus = 0;
                            gameController.SetOnWin();
                        }
                    }
                    else if(movementType == 3)
                    {
                        PlaceOnDefaultPosition();
                    }
                    else if(movementType == 4)
                    {
                        ResetBalls();
                    }
                }
            }
            else if(animationStatus == 2)
            {
                top.transform.position = previousTopPosition + new Vector3(0, 0, movZ) * animationProgress;
            }
            else if(animationStatus == 3)
            {
                arm.transform.position = previousArmPosition + new Vector3(movX, 0, 0) * animationProgress;
            }
            else if(animationStatus == 4)
            {
                arm.transform.localScale = new Vector3(1, 1 + scaleFactor * animationProgress, 1);
            }
            else if(animationStatus == 5)
            {
                arm.transform.localScale = new Vector3(1, 1 + scaleFactor * (1 - animationProgress), 1);
            }
        }
    }

}
