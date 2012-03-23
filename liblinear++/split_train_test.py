#! /usr/bin/python
import sys, os, random

################# Constants   #######################

BLOCK_NUM= 10000
# read this number each time from the input file
#####################################################

if len(sys.argv)!=3:
    print "\n Usage %s input_file percentage_of_trainiging \n This program will split the input file into input_file.train and input_file.test, with the the training file has percentage_of_trainiging of the input_file \n" % (sys.argv[0])
    sys.exit(-1)
    
input_file_name = sys.argv[1]
p = float(sys.argv[2])
assert(p>=0 and p<=1)


input_file= open(input_file_name, 'r')
output_train_file = open(input_file_name+'.train', 'w')
output_test_file = open(input_file_name+'.test', 'w')

while 1:
    lines = input_file.readlines(BLOCK_NUM)
    train_lines = []
    test_lines = []
    if len(lines)==0:
        break
    for single_line in lines:
        if (random.random()< p):
            train_lines.append(single_line)
        else:
            test_lines.append(single_line)
    
    for single_line in train_lines:
        print>>output_train_file, single_line,
        
    for single_line in test_lines:
        print>>output_test_file, single_line,        
        

input_file.close()
output_train_file.close()
output_test_file.close()        