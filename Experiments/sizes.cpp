#include <stdio.h>
using namespace std;

int main () {
  int i;
  long l;
  short s;
  long long ll;
  unsigned int ui;

  printf("int - %d\nlong - %d\nlong long - %d\n", 
    sizeof(int), sizeof(long), sizeof(long long));
  return 0;
}
