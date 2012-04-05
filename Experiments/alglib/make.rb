#! /usr/bin/env ruby
trees = [2, 3, 4, 5]
features = [1000, 2000, 3000]
training_size = [10, 50, 100, 150, 200, 250, 300, 350]

features.product(training_size).each do |f, t| 
  printf "experiment.%d.%d:\n", f, t
  trees.each { |tree| printf "\t./gesturetrain %d %d %d\n", f, t, tree } 
end
