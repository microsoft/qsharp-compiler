int foo(int x)
{
  return x;
}

void bar(int x, int y)
{
  foo(x + y);
}

int main()
{
  foo(2);
  bar(3, 2);

  return 0;
}
