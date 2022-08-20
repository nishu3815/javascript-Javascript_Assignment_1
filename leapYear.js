function isLeapYear (year)
{
    if(year%4==0)
{
    if(year%400==0)
    console.log("Leap Year");

    else if(year%100==0)
    console.log("Not a Leap Year");

    else if(year%100==0)
    console.log("Leap Year");

}
else 
    console.log("Not a Leap Year");
}

let year=3001;
isLeapYear(year);