#include <iostream>
#include <fstream>
#include <string>
#include <algorithm>

#include "dataanalysis.h"

using namespace std;


void readsvmfile(double * container, int lines, int stride, char * filename) {
  int counter = 0;
  string line, part;
  int feature;
  float label, value;
  
  ifstream ifile (filename);
  
  if (ifile.is_open()) {
    while (ifile.good()) {
      if (counter == lines) break;
      
      getline(ifile, line);
     
      sscanf(line.c_str(), "%f", &label);
      if (line.find(' ') == string::npos) line = "";
      else line = line.substr(line.find(' ') + 1); 
      
      // XXX: Fix this for the general case.
      if (label == -1) label = 0;
      container[(counter*stride) + stride - 1] = label; 
      
      while (!line.empty()) {
        if (line.find(' ') == string::npos) part = line;
        else part = line.substr(0, line.find(' '));
        
        sscanf(part.c_str(), "%d:%f", &feature, &value);
 
        //printf("Part: %s, Line: %s\n", part.c_str(), line.c_str());
        
        //printf("Setting [%d] = %f...", feature, value);
        //fflush(stdout);
        container[(counter*stride) + feature - 1] = value;
        //printf("set.\n");

        if (part.length() == line.length()) line = "";
        else line = line.substr(part.length() + 1);  
       
      }
      
      //printf("Processed line %d\n", counter);
      counter ++;
    }
  }
}

int main () {
  cout << "Welcome." << endl;

  alglib::dfreport rep;
  alglib::decisionforest df;
  alglib::ae_int_t info; 
  
  cout << "Created forest, and report." << endl;
  
  int features = 9947;
  double * _train = new double [2000*(features + 1)];
  double * _test = new double [600*(features + 1)];
  
  readsvmfile(_train, 2000, features + 1, "train.dat");
  readsvmfile(_test, 600, features + 1, "test.dat");
  
  alglib::real_2d_array train;
  alglib::real_2d_array test;
  
  train.setcontent(2000, features + 1, _train);
  test.setcontent(600, features + 1, _test);

  cout << "Training." << endl;
  alglib::dfbuildrandomdecisionforest(train, 2000, features, 2, 3, 1, info, df, rep); 
   
  double error = alglib::dfavgrelerror(df, test, 600);
  printf("Average error is %f\n", error);
  
  // Can serialize using alglib::dfserialize and dfunserialize
  string serialized;
  alglib::dfserialize(df, serialized);
  ofstream ofile("serialized.model");
  ofile << serialized;
  ofile.close();
  return 0;
}
