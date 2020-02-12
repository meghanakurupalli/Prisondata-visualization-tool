library('jsonlite')
library('foreach')
library('caret')
library('here')
library('prisonbrief')
library('ggplot2')
library('tidyverse')

json <- fromJSON(txt = here("countries.json")) #import 
listicle <- unique(json) #list of all individual country datapoints
country_df <- NULL
abbreviate <- names(json) #list of names of each country datapoint, we want to be column of dataframe
count <- 1
foreach(l = listicle) %do% {
  abbr <- abbreviate[count]
  if(l$rate < 100){
    rate_factor <- "low"
  }
  else if(l$rate > 200){
    rate_factor <- "high"
  }
  else{
    rate_factor <- "medium"
  }
  country_df <- rbind(country_df, data.frame("abbr" = abbr, "country" = l$country,
                                             "pop" = as.numeric(l$pop), "rate" = as.numeric(l$rate),
                                             "females" = as.numeric(l$females), "juveniles" = as.numeric(l$juveniles), 
                                             "occupancy" = as.numeric(l$occupancy), "government" = as.character(l$government), "rate_factor"= as.factor(rate_factor)
  ))
  count <- count+1
}
country_df <- na.omit(country_df)
write.csv(country_df, file="../Desktop/csv_from_json.csv")

prison_brief_df <- wpb_tables_data %>%
  select(-geometry) #remove the geometry table, which has data we do not want
write.csv(prison_brief_df, file="../Desktop/csv_from_rdata.csv")

#Merge the two datasets on "country"
prison_sub_df <- data.frame("country" = prison_brief_df$country, "pretrial" = prison_brief_df$pre_trial_detainees, "institutions" = prison_brief_df$number_institutions) 
ljoin_df <- dplyr::left_join(country_df, prison_sub_df)
ljoin_df <- na.omit(ljoin_df)
write.csv(ljoin_df, file="../Desktop/merged_dataframe.csv")

knn_df <- country_df %>%
  select(-rate, -abbr) #remove abbrevition of country name and actual rate number so they do not influence KNN training
intrain <- createDataPartition(y = knn_df$rate_factor, p=0.7, list = FALSE)
#createDataPartition() is returning a matrix "intrain" with record's indices
training <- knn_df[intrain,] #create training set for training the algorithm
testing <- knn_df[-intrain,] #create testing set to test the training against for accuracy determination
#define the settings for data split and repeats for the algorithm
#cross-validation, 10 folds, repeated 3 times
trctrl <- trainControl(method = "repeatedcv", number = 10, repeats = 3)
set.seed(411) #this makes the work replicable as long as you use the same seed the data will be split identically each time

knn <- train(rate_factor ~., data = training, method = "knn", tuneLength = 5, trControl=trctrl) 
#tunelength tells the function how many k's to try, then it picks the best performer

test_pred <- predict(knn, newdata = testing) #compare predictions against actual results to get accuracy
varImp(knn) #important variables to the algorithm
#subset wpb dataset to just the highest rate levels, over the last ~12 years
new2 <- subset(wpb_series_data, prison_population_rate >= 500)
g <- ggplot(data= new2, aes(x= country, y= prison_population_rate, fill=year)) +
  geom_bar(colour="black", stat="identity")

#just the top 20 countries with the highest rates, looking at different graphs of interest
a <- arrange(prison_brief_df, desc(prison_population_rate))
pbd <- a[1:20,]
#just a choice of 10 of the top 20 countries with highest rates
countries <- c("Seychelles", "united-states-america", "el-salvador", "Turkmenistan", "Maldives", "Cuba", "Thailand", "russian-federation", "Panama", "Belize")
df <- NULL
foreach(c = countries) %do% {
  b <- wpb_series(country = c)
  df <- rbind(df, data.frame(b))
}
#some example graphs created using ggplot2
#graph1 - rates compared to number of institutions, United States has second highest rate but signicantly higher # of institutions
bp1<- ggplot(data=pbd, aes(x=country, y=prison_population_rate, fill=number_institutions))+
  geom_bar(width = 1, stat = "identity")
bp1 + theme(axis.text.x = element_text(angle = 90, hjust = 1))
#graph2 - see the similarity in rates compared to total prison population
bp2<- ggplot(data=pbd, aes(x=country, y=prison_population_rate, fill=prison_population_total))+
  geom_bar(width = 1, stat = "identity")
bp2 + theme(axis.text.x = element_text(angle = 90, hjust = 1))
#graph
bp3<- ggplot(df, aes(Year, Prison.population.rate, color=Prison.population.rate )) +
  geom_line(size=1.5) + 
  facet_grid(. ~ Country)

