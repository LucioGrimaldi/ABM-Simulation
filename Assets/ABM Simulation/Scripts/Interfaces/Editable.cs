interface Editable
{
    bool Move(float x, float y, float z);
    bool RotateX(float rotation);
    bool RotateY(float rotation);
    bool RotateZ(float rotation);
    bool Scale(float multiplier);
    bool Remove();
}
