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
      
      if (counter % 1000 == 0) {
        float percent = (float)counter/(float)lines;
        printf("\tDone reading %f (%d/%d)\n", percent, counter, lines);
      }
      
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
  
  cout << "Creating containers." << endl;
  int features = 2000;
  

  //int num_train = 121722;
  //int num_test = 30276;
  //char * training_file = "FeatureVectorBlue73.txt.train";
  //char * test_file = "FeatureVectorBlue73.txt.test";
  
  //int num_train = 238035;
  //int num_test = 59964;
  //char * training_file = "FeatureVectorBlue149.txt.train";
  //char * test_file = "FeatureVectorBlue149.txt.test";
  

  int num_train = 653431;
  int num_test = 163214;
  char * training_file = "FeatureVectorBlueDefault.400.txt.train";
  char * test_file = "FeatureVectorBlueDefault.400.txt.test";
  
  cout << "Reading in SVM files." << endl;
  double * _train = new double [num_train*(features + 1)];
  alglib::real_2d_array train;
  readsvmfile(_train, num_train, features + 1, training_file);
  train.setcontent(num_train, features + 1, _train);
  delete [] _train;
  
  double * _test = new double [num_test*(features + 1)];
  alglib::real_2d_array test;
  readsvmfile(_test, num_test, features + 1, test_file);
  test.setcontent(num_test, features + 1, _test);
  delete [] _test;
  
  cout << "Training." << endl;
  alglib::dfbuildrandomdecisionforest(train, num_train, features, 5, 3, 0.66, info, df, rep); 
  
  //cout << "DF has n classes: " << df->nclasses << endl; 
  

  cout << "Testing." << endl;
  double error = alglib::dfavgrelerror(df, test, num_test);
  printf("Average error is %f\n", error);
  
  
  string serialized;
  alglib::dfserialize(df, serialized);
  ofstream ofile ("rf.model");
  ofile << serialized;
  ofile.close();

  // Can serialize using alglib::dfserialize and dfunserialize
  return 0;
}
