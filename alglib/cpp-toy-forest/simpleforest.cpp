#include <iostream>
#include "dataanalysis.h"

using namespace std;



int main () {
  cout << "Welcome." << endl;

  alglib::dfreport rep;
  alglib::decisionforest df;
  alglib::ae_int_t info; 
  
  cout << "Created forest, and report." << endl;
  
  alglib::real_2d_array train("[[0,1,0,1],[0,0,1,0],[0,0,1,0]]");
  
  cout << "Created training set." << endl;
  
  alglib::real_2d_array test = "[[0,1,0,1]]";
  alglib::real_2d_array unknown = "[[0,0,1]]";
  
  cout << "Training." << endl;
  alglib::dfbuildrandomdecisionforest(train, 3, 3, 2, 3, 1, info, df, rep); 
  
  
  double error = alglib::dfavgrelerror(df, test, 1);
  cout << "Average error is " << error << endl;
  // Can serialize using alglib::dfserialize and dfunserialize
  return 0;
}
