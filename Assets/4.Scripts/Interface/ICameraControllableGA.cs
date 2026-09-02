// GA에 달아줄 카메라 조정 가능하게 하는 인터페이스
// 해당 GA가 실행될 때, CameraActionType 타입으로 카메라를 조정하게 한다.
internal interface ICameraControllableGA
{
    public ECameraActionType CameraActionType { get; }
}
