# ssp-assignment
Queue-driven image pipeline on Azure Container Apps. An HTTP endpoint kicks off a job that pulls live Buienradar weather data, fans out to one job per weather station, stamps the readings onto a public image, and stores the results in Blob Storage. Inholland Cloud Computing assessment.
